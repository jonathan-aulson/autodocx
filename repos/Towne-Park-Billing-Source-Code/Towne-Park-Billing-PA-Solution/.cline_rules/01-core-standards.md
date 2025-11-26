CLINE Power Automate Ruleset
============================

_Reducing development time and closing the business-to-implementation gap_

1 Purpose of These Rules
------------------------

These rules tell **CLINE**, the programming-assistant agent, exactly how to turn written **business requirements** into well-structured Power Automate **Cloud Flows** and managed solutions.They enforce consistency, traceability, and maintainability so that developers, architects, and business analysts all read the same “language.”

2 Golden Workflow
-----------------

StepCLINE ActionOutput1**Capture Requirement** – read the user story / process rule.“Business Rule Card” (ID, short title, description, owner).2**Map to Flow Skeleton** – decide: _Instant / Automated / Scheduled?_Flow name, trigger type, main actions list.3**Name Components** (see §3).Draft JSON stub (trigger + top-level scopes).4**Identify Config** – everything environment-specific → Environment Variables.List of variables with schema names.5**Apply Templates** – insert standard error handling, logging scopes, and recurrence controls.Complete flow definition ready to test.6**Document** (see §6).Flow description and README entry.7**Package & Commit** – add to solution, run Solution Checker, pac solution unpack, push to Git.Pull-request with diff and auto-generated changelog.

_If any step fails, CLINE loops back until the validation passes._

3 Naming Standards (all lower ASCII, no spaces)
-----------------------------------------------

ArtifactPatternExample**Solution** or OrderProcessing**Flow Display Name** – ()Orders – Send Confirmation (Automated)**Flow File**\-.jsonSendConfirmation-001C5FAB.json**Trigger**When\_\_ or On\_\_When\_Order\_Created**Action**\_\[\_\]Get\_Customer\_ByID**Variable**vCamelCaseNamevTotalAmount**Scope**Scope\_Scope\_ErrorHandling**Environment Variable (schema)**\_ord\_API\_BaseURL**Connection Reference**\-SvcAcct-SharePoint

4 Flow Structure Blueprint
--------------------------

Plain textANTLR4BashCC#CSSCoffeeScriptCMakeDartDjangoDockerEJSErlangGitGoGraphQLGroovyHTMLJavaJavaScriptJSONJSXKotlinLaTeXLessLuaMakefileMarkdownMATLABMarkupObjective-CPerlPHPPowerShell.propertiesProtocol BuffersPythonRRubySass (Sass)Sass (Scss)SchemeSQLShellSwiftSVGTSXTypeScriptWebAssemblyYAMLXML`   {    "displayName": " –  ()",    "definition": {      "triggers": {        "": { /* Trigger details */ }      },      "actions": {        "Scope_Initialization": {          "type": "Scope",          "actions": { /* … */ }        },        "Scope_MainProcess": {          "type": "Scope",          "actions": { /* … */ }        },        "Scope_ErrorHandling": {          "type": "Scope",          "runAfter": { "Scope_MainProcess": ["Failed"] }        }      }    },    "parameters": {      "": "@parameters('$connections')[\"\"]"    }  }   `

> **Rule** CLINE must always include the three core scopes and ensure Scope\_ErrorHandling runs on failure.

5 Environment Variable Policy
-----------------------------

1.  **Required** for every hard-coded value that differs by environment (URLs, IDs, email addresses, feature flags).
    
2.  **Never store secrets** (use Key Vault or connector credentials).
    
3.  Schema name uses solution prefix; description explains business meaning.
    
4.  Default value is optional and non-sensitive.
    
5.  After import, flows must **fail loudly** if required variables are unset.
    

6 Documentation Mandate
-----------------------

### 6.1 Flow Description Template

Plain textANTLR4BashCC#CSSCoffeeScriptCMakeDartDjangoDockerEJSErlangGitGoGraphQLGroovyHTMLJavaJavaScriptJSONJSXKotlinLaTeXLessLuaMakefileMarkdownMATLABMarkupObjective-CPerlPHPPowerShell.propertiesProtocol BuffersPythonRRubySass (Sass)Sass (Scss)SchemeSQLShellSwiftSVGTSXTypeScriptWebAssemblyYAMLXML`Purpose:   Business Rule ID:   Owner:   Last Modified:   Dependencies:` 

### 6.2 README Sections (auto-generated)

*   **Flows** – table with name, trigger, business-rule ID, last change.
    
*   **Environment Variables** – table with schema, purpose, default value, required (Y/N).
    
*   **Deployment Checklist** – import, configure connections, set variable values, turn on flows.
    
*   **Changelog** – append version entry automatically from commit message.
    

7 Quality Gates
---------------

GateToolPass CriteriaLintCustom JSON linterAll names follow §3 patterns.Solution CheckerMicrosoftNo **Critical** or **High** findings.Unit Test (optional)Flow test harnessKey branches execute without unhandled errors.Code ReviewPull RequestAt least one approver from CoE.

_CLINE blocks merge if any gate fails._

8 Error-Handling & Logging Rules
--------------------------------

1.  **Scope\_ErrorHandling** must:
    
    *   Gather triggerOutputs(), outputs('Scope\_MainProcess'), and workflow().run metadata.
        
    *   Send summary to central logging (Dataverse table or Application Insights).
        
2.  **Retry Policies** – set for external HTTP actions (count = 3, exponential back-off).
    
3.  **Timeouts** – explicit on long-running actions; avoid infinite waits.
    
4.  **Terminate Action** – always end failed flow with status **Failed** and custom message for faster diagnosis.
    

9 Versioning
------------

*   Solution version **Major.Minor.Patch.Build** (auto-increment **Patch** via pipeline).
    
*   Flow changes that alter logic bump **Minor**; cosmetic or doc only = **Patch**.
    
*   Version history stored in README and commit tags (e.g., v1.3.0).
    

10 Bridging Business–Tech Gap
-----------------------------

1.  **Business Rule Cards** captured in Azure DevOps / Jira with ID = BR-###.
    
2.  CLINE embeds that ID:
    
    *   In flow description (Business Rule ID: BR-###).
        
    *   As a tag on the flow in the solution.
        
3.  For every BR, CLINE outputs:✦ **Flow Skeleton** (JSON)✦ **Mapping Table** (Business term → Flow variable / action)
    
4.  Review meeting: Dev + Analyst verify the mapping before implementation proceeds.
    
5.  When the flow publishes, CLINE posts run-history URL back to the task for traceability.
    

11 Time-Saving Shortcuts for CLINE
----------------------------------

*   **Snippet Library** – canned JSON snippets for common connectors (SharePoint CRUD, SQL query, HTTP call with bearer token).
    
*   **Auto-Rename** – after adding an action, immediately rename according to §3 pattern.
    
*   **Template Import** – drag-and-drop error-handling Scope from repository.
    
*   “Create an automated flow: _When Invoice is Created_ → _Get Customer_, _Send Email_. Use env var inv\_SenderEmail.”
    

12 Non-Negotiables
------------------

*   No default action names left in final flows.
    
*   No hard-coded tenant URLs or IDs.
    
*   Every flow inside a **solution**; no “My Flows” in production.
    
*   Service accounts for all production connections.
    
*   README and Environment Variable tables must be up-to-date at merge time.
    

13 Reference Templates
----------------------

### 13.1 Environment Variable Definition (XML)

Plain textANTLR4BashCC#CSSCoffeeScriptCMakeDartDjangoDockerEJSErlangGitGoGraphQLGroovyHTMLJavaJavaScriptJSONJSXKotlinLaTeXLessLuaMakefileMarkdownMATLABMarkupObjective-CPerlPHPPowerShell.propertiesProtocol BuffersPythonRRubySass (Sass)Sass (Scss)SchemeSQLShellSwiftSVGTSXTypeScriptWebAssemblyYAMLXML            `100000000    https://api.dev.orders.example.com    0`

### 13.2 solution.xml Stub

Plain textANTLR4BashCC#CSSCoffeeScriptCMakeDartDjangoDockerEJSErlangGitGoGraphQLGroovyHTMLJavaJavaScriptJSONJSXKotlinLaTeXLessLuaMakefileMarkdownMATLABMarkupObjective-CPerlPHPPowerShell.propertiesProtocol BuffersPythonRRubySass (Sass)Sass (Scss)SchemeSQLShellSwiftSVGTSXTypeScriptWebAssemblyYAMLXML  `OrderProcessing    1.3.12.0      mycompany      ord`    

### 13.3 customizations.xml Sections

Plain textANTLR4BashCC#CSSCoffeeScriptCMakeDartDjangoDockerEJSErlangGitGoGraphQLGroovyHTMLJavaJavaScriptJSONJSXKotlinLaTeXLessLuaMakefileMarkdownMATLABMarkupObjective-CPerlPHPPowerShell.propertiesProtocol BuffersPythonRRubySass (Sass)Sass (Scss)SchemeSQLShellSwiftSVGTSXTypeScriptWebAssemblyYAMLXML      
---------------------------------------------

#ItemWhen?Status1**Business Rule Card** created with ID (BR-###) and stored in tracking toolRequirement intake\[ \]2**Flow skeleton** created, trigger type confirmed, main scopes outlinedDesign\[ \]3**Component names** follow §3 naming standardsDesign\[ \]4All **environment‑specific values** identified and mapped to Environment VariablesDesign\[ \]5**Environment Variable Definitions** added to solution with default values (non‑sensitive)Build\[ \]6**Error‑handling scope** implemented with logging & terminate actionBuild\[ \]7**Retry policies** and **timeouts** configured for external callsBuild\[ \]8**README.md** updated (Flows, Env Vars, Deployment Checklist, Changelog)Build\[ \]9**Solution Checker** run – no Critical/High issuesBuild\[ \]10**Lint checks** pass (naming, standards)Build\[ \]11**Unit/branch tests** executed – key paths succeedBuild\[ \]12Solution **exported**, pac solution unpack executed, changes committedBuild\[ \]13**Pull‑request** created and approved by CoE reviewer(s)Code Review\[ \]14**Managed solution imported** into target environmentDeploy\[ \]15**Connections** configured and authenticatedDeploy\[ \]16Environment Variable **Current Values** set in target environmentDeploy\[ \]17**Flows activated** and test run validatedDeploy\[ \]18**Run‑history URL** posted back to tracking taskDeploy\[ \]19**Version tag** pushed (e.g., v1.3.0) and changelog entry addedDeploy\[ \]20**Production monitoring** set up (alerts, logging dashboard)