#!/usr/bin/env python3
"""
PowerBuilder OrcaScript Generator

This utility creates an OrcaScript file that exports all objects from PowerBuilder
.pbl libraries to a directory structure, automatically creating a minimal target.

Features:
- Generates OrcaScript with proper Windows path quoting
- Creates minimal .pbt target automatically (no pre-existing workspace required)
- Supports both offline and SCC-connected exports
- Cross-platform path handling
- Optional execution via OrcaScript executable
- Non-destructive by default with --overwrite option

Usage Examples:
  # Basic offline export from multiple PBLs
  python generate_orcascript.py --pbl app.pbl --pbl shared.pbl --export-dir ./exported

  # Export with SCC provider and custom target
  python generate_orcascript.py --pbl-list pbls.txt --target-name MyApp \\
    --scc-provider "Microsoft Visual SourceSafe" --scc-project "$/MyProject" \\
    --scc-user developer --export-dir ./src_export

  # Generate and run OrcaScript immediately
  python generate_orcascript.py --pbl app.pbl \\
    --orcascript-exe "C:\\Program Files (x86)\\Appeon\\PowerBuilder 21.0\\IDE\\orcascript210.exe" \\
    --run --overwrite

  # Dry run to preview OrcaScript content
  python generate_orcascript.py --pbl app.pbl --dry-run

Post-generation checklist:
1. Run OrcaScript: orcascript210.exe export.orca
2. Check log file: export.log
3. Continue with PB pipeline: python bw_orchestrate.py run-pb --root ./pb_export_out
"""

import argparse
import os
import subprocess
import sys
from pathlib import Path


def load_pbls_from_file(file_path):
    """Load PBL paths from a text file, ignoring blank lines and comments."""
    pbls = []
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            for line_num, line in enumerate(f, 1):
                line = line.strip()
                if line and not line.startswith('#'):
                    pbls.append(line)
    except FileNotFoundError:
        print(f"Error: PBL list file not found: {file_path}")
        sys.exit(1)
    except Exception as e:
        print(f"Error reading PBL list file {file_path}: {e}")
        sys.exit(1)
    return pbls


def normalize_path(path_str):
    """Convert path to absolute normalized path."""
    return Path(path_str).resolve()


def ensure_parent(file_path):
    """Ensure parent directory exists for the given file path."""
    parent = Path(file_path).parent
    parent.mkdir(parents=True, exist_ok=True)
    return parent


def ensure_directory(dir_path):
    """Ensure directory exists."""
    Path(dir_path).mkdir(parents=True, exist_ok=True)


def quote_orca_path(path):
    """Quote a path for OrcaScript with proper Windows escaping."""
    # Convert to absolute Windows-style path and quote
    abs_path = str(normalize_path(path))
    # Replace forward slashes with backslashes for Windows
    win_path = abs_path.replace('/', '\\')
    return f'"{win_path}"'


def build_scc_connect_line(args):
    """Build SCC connect line based on provided arguments."""
    if args.offline:
        return "scc connect offline"
    
    # Check if any SCC parameters are provided
    scc_params = []
    if args.scc_provider:
        scc_params.append(f'provider "{args.scc_provider}"')
    if args.scc_project:
        scc_params.append(f'project "{args.scc_project}"')
    if args.scc_user:
        scc_params.append(f'user "{args.scc_user}"')
    if args.scc_password:
        scc_params.append(f'password "{args.scc_password}"')
    if args.scc_localpath:
        scc_params.append(f'localpath "{args.scc_localpath}"')
    
    if scc_params:
        return "scc connect " + " ".join(scc_params)
    else:
        return "scc connect offline"


def build_orca_text(args, pbls_abs):
    """Build the complete OrcaScript content."""
    # Build library list
    lib_list = ";".join(str(pbl) for pbl in pbls_abs)
    
    # Build SCC connect line
    scc_line = build_scc_connect_line(args)
    
    # Build OrcaScript content
    orca_lines = [
        "start session",
        f"set output log {quote_orca_path(args.log)}",
        scc_line,
        f"create target {quote_orca_path(args.target_pbt)} \"{args.target_name}\" \"application\"",
        f"set liblist \"{lib_list}\"",
        "save",
        f"open target {quote_orca_path(args.target_pbt)} \"{args.target_name}\"",
        f"export to directory {quote_orca_path(args.export_dir)}",
        "export all",
        "close target",
        "end session"
    ]
    
    return "\n".join(orca_lines) + "\n"


def validate_pbls(pbl_paths):
    """Validate PBL files exist and return absolute paths."""
    valid_pbls = []
    for pbl_path in pbl_paths:
        abs_pbl = normalize_path(pbl_path)
        if abs_pbl.exists():
            valid_pbls.append(abs_pbl)
        else:
            print(f"Warning: PBL file not found, skipping: {pbl_path}")
    
    if not valid_pbls:
        print("Error: No valid PBL files found. At least one .pbl file is required.")
        sys.exit(1)
    
    return valid_pbls


def check_file_overwrite(file_path, overwrite_flag, file_type):
    """Check if file exists and handle overwrite logic."""
    if Path(file_path).exists() and not overwrite_flag:
        print(f"Error: {file_type} file already exists: {file_path}")
        print("Use --overwrite to replace existing files.")
        sys.exit(1)


def run_orcascript(orcascript_exe, orca_file):
    """Execute OrcaScript and return exit code."""
    try:
        print(f"Running OrcaScript: {orcascript_exe} {orca_file}")
        result = subprocess.run([orcascript_exe, str(orca_file)], 
                              capture_output=False, text=True)
        return result.returncode
    except FileNotFoundError:
        print(f"Error: OrcaScript executable not found: {orcascript_exe}")
        return 1
    except Exception as e:
        print(f"Error running OrcaScript: {e}")
        return 1


def main():
    parser = argparse.ArgumentParser(
        description="Generate OrcaScript to export PowerBuilder objects from PBL libraries",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  %(prog)s --pbl app.pbl --pbl shared.pbl
  %(prog)s --pbl-list pbls.txt --target-name MyApp --export-dir ./src
  %(prog)s --pbl app.pbl --run --orcascript-exe "C:\\\\PB\\\\orcascript210.exe"
        """)
    
    # PBL inputs
    parser.add_argument('--pbl', action='append', default=[], dest='pbls',
                       help='PBL file path (can be repeated)')
    parser.add_argument('--pbl-list', 
                       help='File containing PBL paths (one per line)')
    
    # Target configuration
    parser.add_argument('--target-name', default='AutoExport',
                       help='Target name (default: AutoExport)')
    parser.add_argument('--target-pbt', default='./pb_auto_export/AutoExport.pbt',
                       help='Target .pbt file path (default: ./pb_auto_export/AutoExport.pbt)')
    
    # Export configuration
    parser.add_argument('--export-dir', default='./pb_export_out',
                       help='Export directory (default: ./pb_export_out)')
    parser.add_argument('--output-orca', default='./export.orca',
                       help='Output OrcaScript file (default: ./export.orca)')
    parser.add_argument('--log', default='./export.log',
                       help='Log file path (default: ./export.log)')
    
    # SCC configuration
    scc_group = parser.add_mutually_exclusive_group()
    scc_group.add_argument('--offline', action='store_true', default=True,
                          help='Use offline SCC mode (default)')
    parser.add_argument('--scc-provider', help='SCC provider name')
    parser.add_argument('--scc-project', help='SCC project path')
    parser.add_argument('--scc-user', help='SCC username')
    parser.add_argument('--scc-password', help='SCC password')
    parser.add_argument('--scc-localpath', help='SCC local path')
    
    # Execution options
    parser.add_argument('--orcascript-exe', 
                       help='Path to OrcaScript executable (e.g., orcascript210.exe)')
    parser.add_argument('--run', action='store_true',
                       help='Execute OrcaScript after generation (requires --orcascript-exe)')
    parser.add_argument('--overwrite', action='store_true',
                       help='Allow overwriting existing files')
    parser.add_argument('--dry-run', action='store_true',
                       help='Print OrcaScript to stdout without writing files')
    
    args = parser.parse_args()
    
    # If any SCC parameter is provided, disable offline mode
    if any([args.scc_provider, args.scc_project, args.scc_user, 
            args.scc_password, args.scc_localpath]):
        args.offline = False
    
    # Collect PBL paths
    all_pbls = args.pbls.copy()
    if args.pbl_list:
        all_pbls.extend(load_pbls_from_file(args.pbl_list))
    
    if not all_pbls:
        print("Error: No PBL files specified. Use --pbl or --pbl-list.")
        sys.exit(1)
    
    # Validate PBL files
    pbls_abs = validate_pbls(all_pbls)
    
    print(f"Found {len(pbls_abs)} valid PBL file(s):")
    for pbl in pbls_abs:
        print(f"  {pbl}")
    
    # Ensure directories exist
    ensure_directory(args.export_dir)
    ensure_parent(args.target_pbt)
    ensure_parent(args.output_orca)
    
    # Check for file overwrites
    if not args.dry_run:
        check_file_overwrite(args.output_orca, args.overwrite, "OrcaScript")
        check_file_overwrite(args.target_pbt, args.overwrite, "Target PBT")
    
    # Build OrcaScript content
    orca_content = build_orca_text(args, pbls_abs)
    
    if args.dry_run:
        print("\nGenerated OrcaScript content:")
        print("=" * 50)
        print(orca_content)
        print("=" * 50)
        return
    
    # Write OrcaScript file
    try:
        with open(args.output_orca, 'w', encoding='utf-8') as f:
            f.write(orca_content)
        print(f"\nOrcaScript generated: {normalize_path(args.output_orca)}")
    except Exception as e:
        print(f"Error writing OrcaScript file: {e}")
        sys.exit(1)
    
    # Summary
    print(f"Target PBT: {normalize_path(args.target_pbt)}")
    print(f"Export directory: {normalize_path(args.export_dir)}")
    print(f"Log file: {normalize_path(args.log)}")
    
    # Execute OrcaScript if requested
    exit_code = 0
    if args.run:
        if not args.orcascript_exe:
            print("Warning: --run specified but --orcascript-exe not provided. Skipping execution.")
        else:
            exit_code = run_orcascript(args.orcascript_exe, args.output_orca)
            if exit_code == 0:
                print("OrcaScript completed successfully.")
            else:
                print(f"OrcaScript exited with code {exit_code}. Check {args.log} for details.")
    
    # Post-generation checklist
    print("\nNext steps:")
    print("1. Run OrcaScript manually:")
    if args.orcascript_exe:
        print(f"   \"{args.orcascript_exe}\" \"{normalize_path(args.output_orca)}\"")
    else:
        print(f"   orcascript210.exe \"{normalize_path(args.output_orca)}\"")
    print(f"2. Check log file: {normalize_path(args.log)}")
    print(f"3. Continue pipeline: python bw_orchestrate.py run-pb --root \"{normalize_path(args.export_dir)}\"")
    
    sys.exit(exit_code)


if __name__ == '__main__':
    main()