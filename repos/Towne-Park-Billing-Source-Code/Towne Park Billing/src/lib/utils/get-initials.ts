/// write a utility function to get the initials of a name
// e.g. "John Doe" => "JD"
// e.g. "Jane" => "J"
export function getInitials(name: string): string {
  return name
    .split(" ")
    .map((part) => part.charAt(0))
    .join("");
}
