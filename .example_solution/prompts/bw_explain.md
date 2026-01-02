"""You are given a deterministic SIR plus enrichment (business_scaffold, interdependencies_slice, extrapolations).
Write descriptive, specific, business-facing sentences using the provided facts and context. Cite evidence strings inline.
Make it helpful. Avoid boilerplate phrasing. Mark educated guesses in a separate section.

Example Output:
{
  "process_name":"...creditcheckservice...",
  "one_line_summary":"...This process implements a REST service called “Credit Score” that accepts a request (such as a person’s SSN), looks up credit information, and returns the credit score data — logging successes and failures along the way....",
  "what_it_does":[ "...Exposes a REST endpoint (/creditscore) that accepts incoming requests (POST)....", "...When a request arrives, the process extracts the relevant input (for example, the SSN)....", "...It calls an internal lookup step (a separate “LookupDatabase” component) to retrieve credit information such as FICOScore, Rating, and number of inquiries....", "...It builds a response from the lookup results and sends that response back to the caller...", "...If something goes wrong, it logs the error and returns an appropriate fault/HTTP error response...."],
  "why_it_matters":[ "...The service lets other systems ask for a customer’s credit information and get back a structured result (score, rating, inquiries)....", "...It includes basic logging so operators can see when a request succeeded or failed and why...."],
  "interfaces":[{"kind":"REST|Service","endpoint":"...","method":"...","evidence":"..."}],
  "invokes":[{"kind":"Process|JDBC|JMS|Other","target":"...","evidence":"..."}],
  "key_inputs":[ "...SSN..."], "key_outputs":[ "...creditScore..."],
  "errors_and_logging":{"errors":[ "..."], "logging":[ "..."]},
  "nontechnical_notes":[ "...This process is part of the credit check workflow...."],
  "traceability":[ "..."],
  "interdependencies":{"related":["...creditcheckservice.Process..."],"calls":["...creditcheckservice.LookupDatabase..."],"called_by":[...],"shared_identifiers_with":[...],"shared_datastores_with":[...]},
  "extrapolations":[{"hypothesis":"...","rationale":"...","hypothesis_score":0.0}]
}

Output JSON:
{
  "process_name": "<from SIR.process_name>",
  "one_line_summary": "...",
  "what_it_does": ["... 4–7 grounded bullets ..."],
  "why_it_matters": ["... 2–3 business bullets ..."],
  "interfaces": [{"kind":"REST|SOAP|JMS|Timer|Other","endpoint":"...","method":"...","operation":"...","evidence":"..."}],
  "invokes": [{"kind":"Process|JDBC|SOAP|REST|JMS|Other","target":"...","operation":"...","evidence":"..."}],
  "key_inputs": ["..."],
  "key_outputs": ["..."],
  "errors_and_logging": {"errors": ["..."], "logging": ["..."]},
  "traceability": ["...evidence strings..."],
  "interdependencies": {
    "related": ["..."],
    "calls": ["..."],
    "called_by": ["..."],
    "shared_identifiers_with": ["..."],
    "shared_datastores_with": ["..."]
  },
  "extrapolations": [
    {"hypothesis":"...", "rationale":"...", "hypothesis_score":0.0}
  ]
}

"""
