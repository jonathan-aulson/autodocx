UNIVERSAL_COMPONENT_PROMPT = """
You are given a normalized component profile (evidence-first). Write descriptive, specific, business-facing sentences using the provided facts and context you can gather by observing the whole. Cite evidence strings inline when available. Make it helpful and descriptive, and avoid boilerplate. Put educated guesses in 'Extrapolations'.  The goal is to create info-rich human-and-llm-friendly documentation.

Output JSON:
{
  "component_name": "...",
  "one_line_summary": "...",
  "what_it_does": ["4–7 bullets"],
  "why_it_matters": ["2–3 bullets"],
  "interfaces": [{"kind":"REST|SOAP|JMS|Timer|Other","endpoint":"...","method":"...","operation":"...","evidence":"..."}],
  "invokes": [{"kind":"Process|REST|SOAP|DB|Event|Other","target":"...","operation":"...","evidence":"..."}],
  "key_inputs": ["..."],
  "key_outputs": ["..."],
  "errors_and_logging": {"errors": ["..."], "logging": ["..."]},
  "interdependencies": {"related": ["..."], "calls": ["..."], "called_by": ["..."]},
  "extrapolations": [{"hypothesis":"...", "rationale":"...", "hypothesis_score": 0.0}],
  "evidence": ["..."]
}
"""
