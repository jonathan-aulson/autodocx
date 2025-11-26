param connections_sql_1_name string
param townepark_rss_subscriptionid string
param rss_resource_group string
param connections_sharepointonline_name string
param connections_commondataservice_1_name string
param connections_excelonlinebusiness_name string
param connections_office365_name string
param location string 
param workflows_lapp_ready_for_invoicing_eastus2_01_name string
param connections_sharepointonline_externalid string 
param connections_commondataservice_1_externalid string 
param connections_office365_externalid string 
param connections_sql_1_externalid string 
param connections_excelonlinebusiness_externalid string 
param connections_dataverse_url string
param email_recipient string

resource connections_sql_1_name_resource 'Microsoft.Web/connections@2016-06-01' = {
  name: connections_sql_1_name
  location: 'eastus2'
  kind: 'V1'
  properties: {
    displayName: 'TP_STAGE'
    statuses: [
      {
        status: 'Connected'
      }
    ]
    customParameterValues: {}
    createdTime: '2025-05-01T14:33:26.6417424Z'
    changedTime: '2025-05-01T14:58:44.9142519Z'
    api: {
      name: 'sql'
      displayName: 'SQL Server'
      description: 'Microsoft SQL Server is a relational database management system developed by Microsoft. Connect to SQL Server to manage data. You can perform various actions such as create, update, get, and delete on rows in a table.'
      iconUri: 'https://conn-afd-prod-endpoint-bmc9bqahasf3grgk.b01.azurefd.net/releases/v1.0.1746/1.0.1746.4174/sql/icon.png'
      brandColor: '#ba141a'
      id: '/subscriptions/${townepark_rss_subscriptionid}/providers/Microsoft.Web/locations/${location}/managedApis/sql'
      type: 'Microsoft.Web/locations/managedApis'
    }
    testLinks: [
      {
        requestUri: 'https://management.azure.com:443/subscriptions/${townepark_rss_subscriptionid}/resourceGroups/${rss_resource_group}/providers/Microsoft.Web/connections/${connections_sql_1_name}/extensions/proxy/testconnection?api-version=2016-06-01'
        method: 'get'
      }
    ]
  }
}

resource connections_sharepointonline_name_resource 'Microsoft.Web/connections@2016-06-01' = {
  name: connections_sharepointonline_name
  location: location
  kind: 'V1'
  properties: {
    displayName: 'Flow@townepark.com'
    statuses: [
      {
        status: 'Connected'
      }
    ]
    customParameterValues: {}
    nonSecretParameterValues: {}
    createdTime: '2024-09-27T16:09:18.3571079Z'
    changedTime: '2025-04-22T12:53:11.4047326Z'
    api: {
      name: connections_sharepointonline_name
      displayName: 'SharePoint'
      description: 'SharePoint helps organizations share and collaborate with colleagues, partners, and customers. You can connect to SharePoint Online or to an on-premises SharePoint 2016 or 2019 farm using the On-Premises Data Gateway to manage documents and list items.'
      iconUri: 'https://conn-afd-prod-endpoint-bmc9bqahasf3grgk.b01.azurefd.net/releases/v1.0.1745/1.0.1745.4169/${connections_sharepointonline_name}/icon.png'
      brandColor: '#036C70'
      id: '/subscriptions/${townepark_rss_subscriptionid}/providers/Microsoft.Web/locations/${location}/managedApis/${connections_sharepointonline_name}'
      type: 'Microsoft.Web/locations/managedApis'
    }
    testLinks: [
      {
        requestUri: 'https://management.azure.com:443/subscriptions/${townepark_rss_subscriptionid}/resourceGroups/${rss_resource_group}/providers/Microsoft.Web/connections/${connections_sharepointonline_name}/extensions/proxy/datasets?api-version=2016-06-01'
        method: 'get'
      }
    ]
  }
}

resource connections_commondataservice_1_name_resource 'Microsoft.Web/connections@2016-06-01' = {
  name: connections_commondataservice_1_name
  location: location
  kind: 'V1'
  properties: {
    displayName: 'Flow@townepark.com'
    statuses: [
      {
        status: 'Connected'
      }
    ]
    customParameterValues: {}
    nonSecretParameterValues: {
      'token:grantType': 'code'
    }
    createdTime: '2024-09-30T13:23:53.7360936Z'
    changedTime: '2025-04-18T20:11:53.5600274Z'
    api: {
      name: 'commondataservice'
      displayName: 'Microsoft Dataverse'
      description: 'Provides access to the environment database in Microsoft Dataverse.'
      iconUri: 'https://conn-afd-prod-endpoint-bmc9bqahasf3grgk.b01.azurefd.net/releases/v1.0.1735/1.0.1735.4107/commondataservice/icon-la.png'
      brandColor: '#637080'
      id: '/subscriptions/${townepark_rss_subscriptionid}/providers/Microsoft.Web/locations/${location}/managedApis/commondataservice'
      type: 'Microsoft.Web/locations/managedApis'
    }
    testLinks: []
  }
}

resource connections_excelonlinebusiness_name_resource 'Microsoft.Web/connections@2016-06-01' = {
  name: connections_excelonlinebusiness_name
  location: location
  kind: 'V1'
  properties: {
    displayName: 'Flow@townepark.com'
    statuses: [
      {
        status: 'Connected'
      }
    ]
    customParameterValues: {}
    nonSecretParameterValues: {}
    createdTime: '2024-09-30T13:37:58.8830844Z'
    changedTime: '2025-04-22T13:32:13.4647207Z'
    api: {
      name: connections_excelonlinebusiness_name
      displayName: 'Excel Online (Business)'
      description: 'Excel Online (Business) connector lets you work with Excel files in document libraries supported by Microsoft Graph (OneDrive for Business, SharePoint Sites, and Office 365 Groups).'
      iconUri: 'https://conn-afd-prod-endpoint-bmc9bqahasf3grgk.b01.azurefd.net/releases/v1.0.1718/1.0.1718.3946/${connections_excelonlinebusiness_name}/icon.png'
      brandColor: '#107C41'
      id: '/subscriptions/${townepark_rss_subscriptionid}/providers/Microsoft.Web/locations/${location}/managedApis/${connections_excelonlinebusiness_name}'
      type: 'Microsoft.Web/locations/managedApis'
    }
    testLinks: [
      {
        requestUri: 'https://management.azure.com:443/subscriptions/${townepark_rss_subscriptionid}/resourceGroups/${rss_resource_group}/providers/Microsoft.Web/connections/${connections_excelonlinebusiness_name}/extensions/proxy/testconnection?api-version=2016-06-01'
        method: 'get'
      }
    ]
  }
}

resource connections_office365_name_resource 'Microsoft.Web/connections@2016-06-01' = {
  name: connections_office365_name
  location: location
  kind: 'V1'
  properties: {
    displayName: 'Flow@townepark.com'
    statuses: [
      {
        status: 'Connected'
      }
    ]
    customParameterValues: {}
    nonSecretParameterValues: {}
    createdTime: '2024-09-30T14:22:56.6482725Z'
    changedTime: '2025-04-22T13:36:00.1437793Z'
    api: {
      name: connections_office365_name
      displayName: 'Office 365 Outlook'
      description: 'Microsoft Office 365 is a cloud-based service that is designed to help meet your organization\'s needs for robust security, reliability, and user productivity.'
      iconUri: 'https://conn-afd-prod-endpoint-bmc9bqahasf3grgk.b01.azurefd.net/releases/v1.0.1747/1.0.1748.4181/${connections_office365_name}/icon.png'
      brandColor: '#0078D4'
      id: '/subscriptions/${townepark_rss_subscriptionid}/providers/Microsoft.Web/locations/${location}/managedApis/${connections_office365_name}'
      type: 'Microsoft.Web/locations/managedApis'
    }
    testLinks: [
      {
        requestUri: 'https://management.azure.com:443/subscriptions/${townepark_rss_subscriptionid}/resourceGroups/${rss_resource_group}/providers/Microsoft.Web/connections/${connections_office365_name}/extensions/proxy/testconnection?api-version=2016-06-01'
        method: 'get'
      }
    ]
  }
}

resource workflows_lapp_ready_for_invoicing_test_eastus2_01_name_resource 'Microsoft.Logic/workflows@2017-07-01' = {
  name: workflows_lapp_ready_for_invoicing_eastus2_01_name
  location: 'eastus2'
  properties: {
    state: 'Enabled'
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      parameters: {
        IsRequiredToSendEmail: {
          defaultValue: false
          type: 'Bool'
        }
        UseStoredProc: {
          defaultValue: true
          type: 'Bool'
        }
        '$connections': {
          defaultValue: {}
          type: 'Object'
        }
      }
      triggers: {
        When_a_HTTP_request_is_received: {
          type: 'Request'
          kind: 'Http'
          inputs: {
            method: 'POST'
            schema: {
              type: 'object'
              properties: {
                Field_x0020_Comments: {
                  type: 'string'
                }
                Title: {
                  type: 'string'
                }
                Month: {
                  type: 'string'
                }
                Year: {
                  type: 'string'
                }
                Site: {
                  type: 'string'
                }
                Submitter: {
                  type: 'string'
                }
                required: [
                  'Site'
                  'Month'
                  'Year'
                ]
              }
            }
          }
        }
      }
      actions: {
        Condition: {
          actions: {
            Condition_2: {
              actions: {
                'Add_a_new_row_(preview)': {
                  type: 'ApiConnection'
                  inputs: {
                    host: {
                      connection: {
                        name: '@parameters(\'$connections\')[\'commondataservice-1\'][\'connectionId\']'
                      }
                    }
                    method: 'post'
                    body: {
                      cr9e8_comments: '@triggerBody()?[\'Field_x0020_Comments\']'
                      cr9e8_invoicestatus: 'Pending'
                      cr9e8_period: '@{triggerBody()?[\'Year\']}@{triggerBody()?[\'Month\']}'
                      cr9e8_rssfile: '@triggerBody()?[\'Title\']'
                      cr9e8_site: '@substring(triggerBody()?[\'Site\'],2,4)'
                    }
                    headers: {
                      prefer: 'return=representation,odata.include-annotations=*'
                      organization: connections_dataverse_url
                    }
                    path: '/api/data/v9.1/@{encodeURIComponent(encodeURIComponent(\'cr9e8_readyforinvoices\'))}'
                  }
                }
              }
              runAfter: {
                List_rows: [
                  'Succeeded'
                ]
              }
              else: {
                actions: {
                  For_each: {
                    foreach: '@body(\'List_rows\')?[\'value\']'
                    actions: {
                      'Update_a_row_(preview)': {
                        type: 'ApiConnection'
                        inputs: {
                          host: {
                            connection: {
                              name: '@parameters(\'$connections\')[\'commondataservice-1\'][\'connectionId\']'
                            }
                          }
                          method: 'patch'
                          body: {
                            cr9e8_comments: '@triggerBody()?[\'Field_x0020_Comments\']'
                            cr9e8_invoicestatus: 'Updated'
                            cr9e8_period: '@{triggerBody()?[\'Year\']}@{triggerBody()?[\'Month\']}'
                            cr9e8_rssfile: '@triggerBody()?[\'Title\']'
                            cr9e8_site: '@substring(triggerBody()?[\'Site\'],2,4)'
                          }
                          headers: {
                            prefer: 'return=representation,odata.include-annotations=*'
                            accept: 'application/json;odata.metadata=full'
                            organization: connections_dataverse_url
                          }
                          path: '/api/data/v9.1/@{encodeURIComponent(encodeURIComponent(\'cr9e8_readyforinvoices\'))}(@{encodeURIComponent(encodeURIComponent(items(\'For_each\')?[\'cr9e8_readyforinvoiceid\']))})'
                        }
                      }
                    }
                    type: 'Foreach'
                  }
                }
              }
              expression: {
                and: [
                  {
                    equals: [
                      '@length(body(\'List_rows\')[\'value\'])'
                      0
                    ]
                  }
                ]
              }
              type: 'If'
            }
            List_rows: {
              type: 'ApiConnection'
              inputs: {
                host: {
                  connection: {
                    name: '@parameters(\'$connections\')[\'commondataservice-1\'][\'connectionId\']'
                  }
                }
                method: 'get'
                headers: {
                  prefer: 'odata.include-annotations=*'
                  accept: 'application/json;odata.metadata=full'
                  organization: connections_dataverse_url
                }
                path: '/api/data/v9.1/@{encodeURIComponent(encodeURIComponent(\'cr9e8_readyforinvoices\'))}'
                queries: {
                  '$filter': 'cr9e8_rssfile eq \'@{triggerBody()?[\'Title\']}\''
                  '$top': 1
                }
              }
            }
          }
          runAfter: {
            varPeriod: [
              'Succeeded'
            ]
          }
          else: {
            actions: {
              Terminate: {
                type: 'Terminate'
                inputs: {
                  runStatus: 'Succeeded'
                }
              }
            }
          }
          expression: {
            and: [
              {
                contains: [
                  '@triggerBody()?[\'Title\']'
                  'REV'
                ]
              }
              {
                endsWith: [
                  '@triggerBody()?[\'Title\']'
                  '.xlsm'
                ]
              }
            ]
          }
          type: 'If'
        }
        Condition_3: {
          actions: {
            List_rows_2: {
              runAfter: {
                Condition_IsRequiredToSendEmail: [
                  'Succeeded'
                  'TimedOut'
                  'Skipped'
                  'Failed'
                ]
              }
              type: 'ApiConnection'
              inputs: {
                host: {
                  connection: {
                    name: '@parameters(\'$connections\')[\'commondataservice-1\'][\'connectionId\']'
                  }
                }
                method: 'get'
                headers: {
                  prefer: 'odata.include-annotations=*'
                  accept: 'application/json;odata.metadata=full'
                  organization: connections_dataverse_url
                }
                path: '/api/data/v9.1/@{encodeURIComponent(encodeURIComponent(\'cr9e8_readyforinvoices\'))}'
                queries: {
                  '$filter': 'cr9e8_rssfile eq \'@{triggerBody()?[\'Title\']}\''
                  '$top': 1
                }
              }
            }
            For_each_1: {
              foreach: '@body(\'List_rows_2\')?[\'value\']'
              actions: {
                'Update_a_row_(preview)_2': {
                  type: 'ApiConnection'
                  inputs: {
                    host: {
                      connection: {
                        name: '@parameters(\'$connections\')[\'commondataservice-1\'][\'connectionId\']'
                      }
                    }
                    method: 'patch'
                    body: {
                      cr9e8_invoicestatus: 'Ready for Invoice'
                    }
                    headers: {
                      prefer: 'return=representation,odata.include-annotations=*'
                      accept: 'application/json;odata.metadata=full'
                      organization: connections_dataverse_url
                    }
                    path: '/api/data/v9.1/@{encodeURIComponent(encodeURIComponent(\'cr9e8_readyforinvoices\'))}(@{encodeURIComponent(encodeURIComponent(item()?[\'cr9e8_readyforinvoiceid\']))})'
                  }
                }
              }
              runAfter: {
                List_rows_2: [
                  'Succeeded'
                ]
              }
              type: 'Foreach'
            }
            Condition_IsRequiredToSendEmail: {
              actions: {
                'Send_an_email_(V2)': {
                  type: 'ApiConnection'
                  inputs: {
                    host: {
                      connection: {
                        name: '@parameters(\'$connections\')[\'office365\'][\'connectionId\']'
                      }
                    }
                    method: 'post'
                    body: {
                      To: '@variables(\'EmailRecipient\')'
                      Subject: 'End of Month RSS Submission Completed - '
                      Body: '<p class="editor-paragraph">Thank you for submitting your RSS for the period of: -</p><p class="editor-paragraph">No further action is required.</p><br><br><p class="editor-paragraph"><span style="font-size: 10px;">Flow Run-ID: </span>@{workflow().run.id}</p>'
                      Bcc: email_recipient
                      Importance: 'Normal'
                    }
                    path: '/v2/Mail'
                  }
                }
              }
              else: {
                actions: {}
              }
              expression: {
                and: [
                  {
                    equals: [
                      '@parameters(\'IsRequiredToSendEmail\')'
                      true
                    ]
                  }
                ]
              }
              type: 'If'
            }
          }
          runAfter: {
            TPCollected_Loop: [
              'Succeeded'
            ]
          }
          else: {
            actions: {
              For_each_4: {
                foreach: '@body(\'Get_items\')?[\'value\']'
                actions: {
                  Set_RSS_to_Rejected: {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sharepointonline\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        method: 'POST'
                        uri: '/_api/web/lists/GetByTitle(\'Revenue\')/items(@{items(\'For_each_4\')?[\'ID\']})'
                        headers: {
                          'IF-MATCH': '*'
                          'X-HTTP-Method': 'MERGE'
                          'X-RequestDigest': '{form_digest_value}'
                          accept: 'application/json;odata-verbose'
                          'content-type': 'application/json'
                        }
                        body: '{\n"Revenue_x0020_Doc_x0020_Status": "Rejected"\n}'
                      }
                      path: '/datasets/@{encodeURIComponent(encodeURIComponent(concat(\'https://townepark.sharepoint.com/sites/\', triggerBody()?[\'Site\'])))}/httprequest'
                    }
                  }
                }
                runAfter: {
                  Get_items: [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
              }
              For_each_5: {
                foreach: '@body(\'List_rows_3\')?[\'value\']'
                actions: {
                  'Update_a_row_(preview)_3': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'commondataservice-1\'][\'connectionId\']'
                        }
                      }
                      method: 'patch'
                      body: {
                        cr9e8_invoicestatus: 'Rejected'
                      }
                      headers: {
                        prefer: 'return=representation,odata.include-annotations=*'
                        accept: 'application/json;odata.metadata=full'
                        organization: connections_dataverse_url
                      }
                      path: '/api/data/v9.1/@{encodeURIComponent(encodeURIComponent(\'cr9e8_readyforinvoices\'))}(@{encodeURIComponent(encodeURIComponent(item()?[\'cr9e8_readyforinvoiceid\']))})'
                    }
                  }
                }
                runAfter: {
                  List_rows_3: [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
              }
              Get_items: {
                runAfter: {
                  'Send_an_email_(V2)_2': [
                    'Succeeded'
                    'TimedOut'
                    'Skipped'
                    'Failed'
                  ]
                }
                type: 'ApiConnection'
                inputs: {
                  host: {
                    connection: {
                      name: '@parameters(\'$connections\')[\'sharepointonline\'][\'connectionId\']'
                    }
                  }
                  method: 'get'
                  path: '/datasets/@{encodeURIComponent(encodeURIComponent(concat(\'https://townepark.sharepoint.com/sites/\',triggerBody()?[\'Site\'])))}/tables/@{encodeURIComponent(encodeURIComponent(\'Revenue\'))}/items'
                  queries: {
                    '$filter': 'Title eq \'\''
                  }
                }
              }
              'Send_an_email_(V2)_2': {
                type: 'ApiConnection'
                inputs: {
                  host: {
                    connection: {
                      name: '@parameters(\'$connections\')[\'office365\'][\'connectionId\']'
                    }
                  }
                  method: 'post'
                  body: {
                    To: '@variables(\'EmailRecipient\')'
                    Subject: 'End of Month RSS Submission Failure - '
                    Body: '<p class="editor-paragraph">The end of month RSS for Period: - failed as a result of the total deposits recorded in the RSS not matching what was collected and recorded in the accounting system.<br><br>Below is what you submitted vs. what was collected:<br><br>@{formatNumber(div(mul(sub(variables(\'TPDeposit\'), variables(\'TPCash\')), 100), 100), \'0.00\')} - Deposit recorded in RSS<br>@{if(equals(variables(\'TPCollected\'), 0), \'0.00\', formatnumber(mul(variables(\'TPCollected\'), -1), \'0.00\'))\r\n} - Collected from GP<br><br>Please review the General Ledger report available in Towne Vision for your site and balance the recorded deposits to the value in account number 4785 in that report.<br><br>Once balanced, please resubmit your RSS.<br></p><br><p class="editor-paragraph"><span style="font-size: 10px;">Flow Run-ID: </span>@{workflow().run.id}</p>'
                    Bcc: email_recipient
                    Importance: 'Normal'
                  }
                  path: '/v2/Mail'
                }
              }
              List_rows_3: {
                runAfter: {
                  'Execute_stored_procedure_(V2)': [
                    'Succeeded'
                  ]
                }
                type: 'ApiConnection'
                inputs: {
                  host: {
                    connection: {
                      name: '@parameters(\'$connections\')[\'commondataservice-1\'][\'connectionId\']'
                    }
                  }
                  method: 'get'
                  headers: {
                    prefer: 'odata.include-annotations=*'
                    accept: 'application/json;odata.metadata=full'
                    organization: connections_dataverse_url
                  }
                  path: '/api/data/v9.1/@{encodeURIComponent(encodeURIComponent(\'cr9e8_readyforinvoices\'))}'
                  queries: {
                    '$filter': 'cr9e8_rssfile eq \'@{triggerBody()?[\'Title\']}\''
                    '$top': 1
                  }
                }
              }
              'Execute_stored_procedure_(V2)': {
                runAfter: {
                  For_each_4: [
                    'Succeeded'
                  ]
                }
                type: 'ApiConnection'
                inputs: {
                  host: {
                    connection: {
                      name: '@parameters(\'$connections\')[\'sql\'][\'connectionId\']'
                    }
                  }
                  method: 'post'
                  body: {
                    SiteParameter: '@{substring(triggerBody()?[\'Site\'],2,4)}'
                    DateParameter: '@{concat(substring(triggerBody()?[\'title\'], 9, 4), \'-\', substring(triggerBody()?[\'title\'], 15, 2))}'
                  }
                  path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'EDW-NODE-01\\TP_EDW01\'))},@{encodeURIComponent(encodeURIComponent(\'TP_STAGE\'))}/procedures/@{encodeURIComponent(encodeURIComponent(\'[dbo].[spREVENUE_DATAMART_DAILY_INVOICE_DELETE]\'))}'
                }
              }
            }
          }
          expression: {
            or: [
              {
                equals: [
                  '@formatNumber(sub(variables(\'TPDeposit\'), variables(\'TPCash\')), \'0.00\')'
                  '@if(equals(variables(\'TPCollected\'), 0), \'0.00\', formatnumber(mul(variables(\'TPCollected\'), -1), \'0.00\'))'
                ]
              }
              {
                equals: [
                  1
                  1
                ]
              }
            ]
          }
          type: 'If'
        }
        Parse_JSON: {
          runAfter: {
            Send_an_HTTP_request_to_SharePoint: [
              'Succeeded'
            ]
          }
          type: 'ParseJson'
          inputs: {
            content: '@body(\'Send_an_HTTP_request_to_SharePoint\')'
            schema: {
              properties: {
                value: {
                  items: {
                    properties: {
                      createdDateTime: {
                        type: 'string'
                      }
                      description: {
                        type: 'string'
                      }
                      driveType: {
                        type: 'string'
                      }
                      id: {
                        type: 'string'
                      }
                      lastModifiedBy: {
                        properties: {
                          user: {
                            properties: {
                              displayName: {
                                type: 'string'
                              }
                              email: {
                                type: 'string'
                              }
                              id: {
                                type: 'string'
                              }
                            }
                            type: 'object'
                          }
                        }
                        type: 'object'
                      }
                      lastModifiedDateTime: {
                        type: 'string'
                      }
                      name: {
                        type: 'string'
                      }
                      quota: {
                        properties: {
                          deleted: {
                            type: 'integer'
                          }
                          fileCount: {
                            type: 'integer'
                          }
                          remaining: {
                            type: 'integer'
                          }
                          state: {
                            type: 'string'
                          }
                          total: {
                            type: 'integer'
                          }
                          used: {
                            type: 'integer'
                          }
                        }
                        type: 'object'
                      }
                      webUrl: {
                        type: 'string'
                      }
                    }
                    type: 'object'
                  }
                  type: 'array'
                }
              }
              type: 'object'
            }
          }
        }
        Run_script_from_SharePoint_library: {
          runAfter: {
            Parse_JSON: [
              'Succeeded'
            ]
          }
          metadata: {
            '01TSCES72NB3F7CRH42RFLCQDAFEIDSTGI': '/Office Scripts/DataMartDailyExtract_07312024.osts'
          }
          type: 'ApiConnection'
          inputs: {
            host: {
              connection: {
                name: '@parameters(\'$connections\')[\'excelonlinebusiness\'][\'connectionId\']'
              }
            }
            method: 'post'
            path: '/v2/officescripting/api/unattended/run/@{encodeURIComponent(first(body(\'Parse_JSON\')?[\'value\'])?[\'id\'])}/@{encodeURIComponent(encodeURIComponent(triggerBody()?[\'Title\']))}/@{encodeURIComponent(\'b!-T5UgEKoBUuo2RIzcatM0R48rd3b5R1FoY1LRZakrhvQ3WPxvjf6TKQEWLGIyvms\')}/@{encodeURIComponent(encodeURIComponent(\'01TSCES72NB3F7CRH42RFLCQDAFEIDSTGI\'))}'
            queries: {
              source: '@{concat(\'https://townepark.sharepoint.com/sites/\',triggerBody()?[\'Site\'])}'
              scriptSource: 'sites/townepark.sharepoint.com,80543ef9-a842-4b05-a8d9-123371ab4cd1,ddad3c1e-e5db-451d-a18d-4b4596a4ae1b'
            }
          }
        }
        Send_an_HTTP_request_to_SharePoint: {
          runAfter: {
            Condition: [
              'Succeeded'
            ]
          }
          type: 'ApiConnection'
          inputs: {
            host: {
              connection: {
                name: '@parameters(\'$connections\')[\'sharepointonline\'][\'connectionId\']'
              }
            }
            method: 'post'
            body: {
              method: 'GET'
              uri: '_api/v2.1/drives?$filter=name eq \'Revenue\''
            }
            path: '/datasets/@{encodeURIComponent(encodeURIComponent(concat(\'https://townepark.sharepoint.com/sites/\',triggerBody()?[\'Site\'])))}/httprequest'
          }
        }
        TPCollected_Loop: {
          foreach: '@body(\'Get_Collected\')?[\'value\']'
          actions: {
            Increment_varTPCollected: {
              type: 'IncrementVariable'
              inputs: {
                name: 'TPCollected'
                value: '@items(\'TPCollected_Loop\')?[\'BALANCE\']'
              }
            }
          }
          runAfter: {
            TPDeposit_Loop: [
              'Succeeded'
            ]
          }
          type: 'Foreach'
          runtimeConfiguration: {
            concurrency: {
              repetitions: 1
            }
          }
        }
        TPDeposit_Loop: {
          foreach: '@body(\'Get_Deposit\')?[\'value\']'
          actions: {
            Incement_varTPDeposit: {
              type: 'IncrementVariable'
              inputs: {
                name: 'TPDeposit'
                value: '@items(\'TPDeposit_Loop\')?[\'EXTERNALREVENUE\']'
              }
            }
          }
          runAfter: {
            TP_Cash_Loop: [
              'Succeeded'
            ]
          }
          type: 'Foreach'
          runtimeConfiguration: {
            concurrency: {
              repetitions: 1
            }
          }
        }
        varTPDeposit: {
          runAfter: {
            varTPCollected: [
              'Succeeded'
            ]
          }
          type: 'InitializeVariable'
          inputs: {
            variables: [
              {
                name: 'TPDeposit'
                type: 'float'
                value: 0
              }
            ]
          }
        }
        varEmailRecipient: {
          runAfter: {
            Get_Submitter_Email: [
              'Succeeded'
              'TimedOut'
              'Skipped'
              'Failed'
            ]
          }
          type: 'InitializeVariable'
          inputs: {
            variables: [
              {
                name: 'EmailRecipient'
                type: 'string'
                value: email_recipient
              }
            ]
          }
        }
        Email_Loop: {
          foreach: '@body(\'Get_Submitter_Email\')?[\'value\']'
          actions: {
            Set_varEmailRecipient: {
              type: 'SetVariable'
              inputs: {
                name: 'EmailRecipient'
                value: '@coalesce(items(\'Email_Loop\')?[\'EMAIL_PRIMARY_WORK\'],\'${email_recipient}\')'
              }
            }
          }
          runAfter: {
            varEmailRecipient: [
              'Failed'
              'Succeeded'
            ]
          }
          type: 'Foreach'
        }
        varPeriod: {
          runAfter: {
            Email_Loop: [
              'Succeeded'
              'TimedOut'
              'Skipped'
              'Failed'
            ]
          }
          type: 'InitializeVariable'
          inputs: {
            variables: [
              {
                name: 'varPeriod'
                type: 'string'
                value: '@{concat(substring(triggerBody()?[\'title\'], 9, 4), \'-\', substring(triggerBody()?[\'title\'], 15, 2))}'
              }
            ]
          }
        }
        varTPCash: {
          runAfter: {
            Get_Deposit: [
              'Succeeded'
            ]
          }
          type: 'InitializeVariable'
          inputs: {
            variables: [
              {
                name: 'TPCash'
                type: 'float'
                value: 0
              }
            ]
          }
        }
        varTPCollected: {
          runAfter: {
            varTPCash: [
              'Succeeded'
            ]
          }
          type: 'InitializeVariable'
          inputs: {
            variables: [
              {
                name: 'TPCollected'
                type: 'float'
                value: 0
              }
            ]
          }
        }
        TP_Cash_Loop: {
          foreach: '@body(\'Get_Cash\')?[\'value\']'
          actions: {
            Increment_varTPCash: {
              type: 'IncrementVariable'
              inputs: {
                name: 'TPCash'
                value: '@items(\'TP_Cash_Loop\')?[\'VALUE\']'
              }
            }
          }
          runAfter: {
            varTPDeposit: [
              'Succeeded'
            ]
          }
          type: 'Foreach'
          runtimeConfiguration: {
            concurrency: {
              repetitions: 1
            }
          }
        }
        'Execute_stored_procedure_(V2)-_PreCopy_Script_to_Puge_pre-existing_data_1': {
          runAfter: {
            Run_script_from_SharePoint_library: [
              'Succeeded'
            ]
          }
          type: 'ApiConnection'
          inputs: {
            host: {
              connection: {
                name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
              }
            }
            method: 'post'
            body: {
              SiteParameter: '@substring(triggerBody()?[\'Site\'],2,4)'
              DateParameter: '@concat(substring(triggerBody()?[\'title\'], 9, 4), \'-\', substring(triggerBody()?[\'title\'], 15, 2))'
            }
            path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/procedures/@{encodeURIComponent(encodeURIComponent(\'[dbo].[spREVENUE_DATAMART_DAILY_INVOICE_DELETE]\'))}'
          }
        }
        Get_Cash: {
          runAfter: {
            If_UsedStoredProc: [
              'Succeeded'
            ]
          }
          type: 'ApiConnection'
          inputs: {
            host: {
              connection: {
                name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
              }
            }
            method: 'get'
            path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
            queries: {
              '$filter': 'SITE eq \'@{substring(triggerBody()?[\'Site\'],2,4)}\' and REVENUE_CATEGORY eq \'Cash Deposits to Local TP Bank Account\' and startswith(DATE,\'@{variables(\'varPeriod\')}\')'
            }
          }
        }
        Get_Collected: {
          runAfter: {
            Get_Cash: [
              'Succeeded'
            ]
          }
          type: 'ApiConnection'
          inputs: {
            host: {
              connection: {
                name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
              }
            }
            method: 'get'
            path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[ACCOUNT_SUMMARY]\'))}/items'
            queries: {
              '$filter': 'COST_CENTER eq \'@{substring(triggerBody()?[\'Site\'],2,4)}\' and PERIOD eq @{concat(substring(triggerBody()?[\'title\'], 9, 4), substring(triggerBody()?[\'title\'], 15, 2))} and MAIN_ACCOUNT eq \'4785\''
            }
          }
        }
        Get_Deposit: {
          runAfter: {
            Get_Collected: [
              'Succeeded'
            ]
          }
          type: 'ApiConnection'
          inputs: {
            host: {
              connection: {
                name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
              }
            }
            method: 'get'
            path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[vwREVENUE_DAILY_DETAIL_INVOICE]\'))}/items'
            queries: {
              '$filter': 'SITE eq \'@{substring(triggerBody()?[\'Site\'],2,4)}\' and DEPOSIT_FLAG eq \'Y\' and startswith(DATE,\'@{variables(\'varPeriod\')}\')'
            }
          }
        }
        Get_Submitter_Email: {
          runAfter: {}
          type: 'ApiConnection'
          inputs: {
            host: {
              connection: {
                name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
              }
            }
            method: 'get'
            path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[WORKDAY_SITE_SALARIES]\'))}/items'
            queries: {
              '$filter': 'FULL_LEGAL_NAME eq \'@{replace(concat(\n    trim(split(triggerBody()?[\'Submitter\'], \',\')[1]), \n    \' \', \n    trim(split(triggerBody()?[\'Submitter\'], \',\')[0])\n), \'\'\'\', \'\'\'\'\'\')\n}\''
              '$top': 1
              '$select': 'EMAIL_PRIMARY_WORK'
            }
          }
        }
        If_UsedStoredProc: {
          actions: {
            Build_Insert_JSON: {
              type: 'Select'
              inputs: {
                from: '@body(\'Run_script_from_SharePoint_library\')?[\'result\']'
                select: {
                  FILE_NAME: '@triggerBody()?[\'Title\']'
                  SITE: '@coalesce(item()?[0], null)'
                  DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                  REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                  REVENUE_CATEGORY: '@coalesce(item()?[3])'
                  VALUE_TYPE: '@coalesce(item()?[4])'
                  VALUE: '@coalesce(item()?[5],0.00)'
                  IS_DRAFT: '@false'
                }
              }
            }
            'Execute_stored_procedure_[Bulk_Insert]': {
              runAfter: {
                Build_Insert_JSON: [
                  'Succeeded'
                ]
              }
              type: 'ApiConnection'
              inputs: {
                host: {
                  connection: {
                    name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                  }
                }
                method: 'post'
                body: {
                  Json: '@{body(\'Build_Insert_JSON\')}'
                }
                path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/procedures/@{encodeURIComponent(encodeURIComponent(\'[dbo].[spInsertJSON_REVENUE_DATAMART_DAILY_INVOICE]\'))}'
              }
            }
          }
          runAfter: {
            'Execute_stored_procedure_(V2)-_PreCopy_Script_to_Puge_pre-existing_data_1': [
              'Succeeded'
            ]
          }
          else: {
            actions: {
              'SQL_Insert_1-250': {
                foreach: '@take(body(\'Run_script_from_SharePoint_library\')?[\'result\'],250)'
                actions: {
                  Delay: {
                    runAfter: {
                      'Insert_row_(V2)_14': [
                        'Succeeded'
                      ]
                    }
                    type: 'Wait'
                    inputs: {
                      interval: {
                        count: 1
                        unit: 'Second'
                      }
                    }
                  }
                  'Insert_row_(V2)_14': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        DW_LOADED_DTTM: '@utcNow()'
                        FILE_NAME: '@triggerBody()?[\'Title\']'
                        SITE: '@coalesce(item()?[0], null)'
                        DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                        IS_DRAFT: false
                        REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                        REVENUE_CATEGORY: '@coalesce(item()?[3])'
                        VALUE_TYPE: '@coalesce(item()?[4])'
                        VALUE: '@coalesce(item()?[5],0.00)'
                      }
                      path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
                    }
                  }
                }
                type: 'Foreach'
                runtimeConfiguration: {
                  concurrency: {
                    repetitions: 3
                  }
                }
              }
              'SQL_Insert_251-500': {
                foreach: '@take(skip(body(\'Run_script_from_SharePoint_library\')?[\'result\'], 250), 250)\r\n'
                actions: {
                  Delay_8: {
                    runAfter: {
                      'Insert_row_(V2)': [
                        'Succeeded'
                      ]
                    }
                    type: 'Wait'
                    inputs: {
                      interval: {
                        count: 1
                        unit: 'Second'
                      }
                    }
                  }
                  'Insert_row_(V2)': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        DW_LOADED_DTTM: '@utcNow()'
                        FILE_NAME: '@triggerBody()?[\'Title\']'
                        SITE: '@coalesce(item()?[0], null)'
                        DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                        IS_DRAFT: false
                        REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                        REVENUE_CATEGORY: '@coalesce(item()?[3])'
                        VALUE_TYPE: '@coalesce(item()?[4])'
                        VALUE: '@coalesce(item()?[5],0.00)'
                      }
                      path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
                    }
                  }
                }
                runAfter: {
                  'SQL_Insert_1-250': [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
                runtimeConfiguration: {
                  concurrency: {
                    repetitions: 3
                  }
                }
              }
              'SQL_Insert_501-750': {
                foreach: '@take(skip(body(\'Run_script_from_SharePoint_library\')?[\'result\'], 500), 250)\r\n'
                actions: {
                  Delay_1: {
                    runAfter: {
                      'Insert_row_(V2)_7': [
                        'Succeeded'
                      ]
                    }
                    type: 'Wait'
                    inputs: {
                      interval: {
                        count: 1
                        unit: 'Second'
                      }
                    }
                  }
                  'Insert_row_(V2)_7': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        DW_LOADED_DTTM: '@utcNow()'
                        FILE_NAME: '@triggerBody()?[\'Title\']'
                        SITE: '@coalesce(item()?[0], null)'
                        DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                        IS_DRAFT: false
                        REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                        REVENUE_CATEGORY: '@coalesce(item()?[3])'
                        VALUE_TYPE: '@coalesce(item()?[4])'
                        VALUE: '@coalesce(item()?[5],0.00)'
                      }
                      path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
                    }
                  }
                }
                runAfter: {
                  'SQL_Insert_251-500': [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
                runtimeConfiguration: {
                  concurrency: {
                    repetitions: 3
                  }
                }
              }
              'SQL_Insert_751-1000': {
                foreach: '@take(skip(body(\'Run_script_from_SharePoint_library\')?[\'result\'], 750), 250)\r\n'
                actions: {
                  Delay_9: {
                    runAfter: {
                      'Insert_row_(V2)_2': [
                        'Succeeded'
                      ]
                    }
                    type: 'Wait'
                    inputs: {
                      interval: {
                        count: 1
                        unit: 'Second'
                      }
                    }
                  }
                  'Insert_row_(V2)_2': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        DW_LOADED_DTTM: '@utcNow()'
                        FILE_NAME: '@triggerBody()?[\'Title\']'
                        SITE: '@coalesce(item()?[0], null)'
                        DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                        IS_DRAFT: false
                        REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                        REVENUE_CATEGORY: '@coalesce(item()?[3])'
                        VALUE_TYPE: '@coalesce(item()?[4])'
                        VALUE: '@coalesce(item()?[5],0.00)'
                      }
                      path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
                    }
                  }
                }
                runAfter: {
                  'SQL_Insert_501-750': [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
                runtimeConfiguration: {
                  concurrency: {
                    repetitions: 3
                  }
                }
              }
              'SQL_Insert_1001-1250': {
                foreach: '@take(skip(body(\'Run_script_from_SharePoint_library\')?[\'result\'], 1000), 250)\r\n'
                actions: {
                  Delay_2: {
                    runAfter: {
                      'Insert_row_(V2)_8': [
                        'Succeeded'
                      ]
                    }
                    type: 'Wait'
                    inputs: {
                      interval: {
                        count: 1
                        unit: 'Second'
                      }
                    }
                  }
                  'Insert_row_(V2)_8': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        DW_LOADED_DTTM: '@utcNow()'
                        FILE_NAME: '@triggerBody()?[\'Title\']'
                        SITE: '@coalesce(item()?[0], null)'
                        DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                        IS_DRAFT: false
                        REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                        REVENUE_CATEGORY: '@coalesce(item()?[3])'
                        VALUE_TYPE: '@coalesce(item()?[4])'
                        VALUE: '@coalesce(item()?[5],0.00)'
                      }
                      path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
                    }
                  }
                }
                runAfter: {
                  'SQL_Insert_751-1000': [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
                runtimeConfiguration: {
                  concurrency: {
                    repetitions: 3
                  }
                }
              }
              'SQL_Insert_1251-1500': {
                foreach: '@take(skip(body(\'Run_script_from_SharePoint_library\')?[\'result\'], 1250), 250)\r\n'
                actions: {
                  Delay_10: {
                    runAfter: {
                      'Insert_row_(V2)_3': [
                        'Succeeded'
                      ]
                    }
                    type: 'Wait'
                    inputs: {
                      interval: {
                        count: 1
                        unit: 'Second'
                      }
                    }
                  }
                  'Insert_row_(V2)_3': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        DW_LOADED_DTTM: '@utcNow()'
                        FILE_NAME: '@triggerBody()?[\'Title\']'
                        SITE: '@coalesce(item()?[0], null)'
                        DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                        IS_DRAFT: false
                        REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                        REVENUE_CATEGORY: '@coalesce(item()?[3])'
                        VALUE_TYPE: '@coalesce(item()?[4])'
                        VALUE: '@coalesce(item()?[5],0.00)'
                      }
                      path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
                    }
                  }
                }
                runAfter: {
                  'SQL_Insert_1001-1250': [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
                runtimeConfiguration: {
                  concurrency: {
                    repetitions: 3
                  }
                }
              }
              'SQL_Insert_1501-1750': {
                foreach: '@take(skip(body(\'Run_script_from_SharePoint_library\')?[\'result\'], 1500), 250)'
                actions: {
                  Delay_3: {
                    runAfter: {
                      'Insert_row_(V2)_9': [
                        'Succeeded'
                      ]
                    }
                    type: 'Wait'
                    inputs: {
                      interval: {
                        count: 1
                        unit: 'Second'
                      }
                    }
                  }
                  'Insert_row_(V2)_9': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        DW_LOADED_DTTM: '@utcNow()'
                        FILE_NAME: '@triggerBody()?[\'Title\']'
                        SITE: '@coalesce(item()?[0], null)'
                        DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                        IS_DRAFT: false
                        REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                        REVENUE_CATEGORY: '@coalesce(item()?[3])'
                        VALUE_TYPE: '@coalesce(item()?[4])'
                        VALUE: '@coalesce(item()?[5],0.00)'
                      }
                      path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
                    }
                  }
                }
                runAfter: {
                  'SQL_Insert_1251-1500': [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
                runtimeConfiguration: {
                  concurrency: {
                    repetitions: 3
                  }
                }
              }
              'SQL_Insert_1751-2000': {
                foreach: '@take(skip(body(\'Run_script_from_SharePoint_library\')?[\'result\'], 1750), 250)'
                actions: {
                  Delay_11: {
                    runAfter: {
                      'Insert_row_(V2)_15': [
                        'Succeeded'
                      ]
                    }
                    type: 'Wait'
                    inputs: {
                      interval: {
                        count: 1
                        unit: 'Second'
                      }
                    }
                  }
                  'Insert_row_(V2)_15': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        DW_LOADED_DTTM: '@utcNow()'
                        FILE_NAME: '@triggerBody()?[\'Title\']'
                        SITE: '@coalesce(item()?[0], null)'
                        DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                        IS_DRAFT: false
                        REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                        REVENUE_CATEGORY: '@coalesce(item()?[3])'
                        VALUE_TYPE: '@coalesce(item()?[4])'
                        VALUE: '@coalesce(item()?[5],0.00)'
                      }
                      path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
                    }
                  }
                }
                runAfter: {
                  'SQL_Insert_1501-1750': [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
                runtimeConfiguration: {
                  concurrency: {
                    repetitions: 3
                  }
                }
              }
              'SQL_Insert_2001-2250': {
                foreach: '@take(skip(body(\'Run_script_from_SharePoint_library\')?[\'result\'], 2000), 250)'
                actions: {
                  Delay_4: {
                    runAfter: {
                      'Insert_row_(V2)_16': [
                        'Succeeded'
                      ]
                    }
                    type: 'Wait'
                    inputs: {
                      interval: {
                        count: 1
                        unit: 'Second'
                      }
                    }
                  }
                  'Insert_row_(V2)_16': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        DW_LOADED_DTTM: '@utcNow()'
                        FILE_NAME: '@triggerBody()?[\'Title\']'
                        SITE: '@coalesce(item()?[0], null)'
                        DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                        IS_DRAFT: false
                        REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                        REVENUE_CATEGORY: '@coalesce(item()?[3])'
                        VALUE_TYPE: '@coalesce(item()?[4])'
                        VALUE: '@coalesce(item()?[5],0.00)'
                      }
                      path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
                    }
                  }
                }
                runAfter: {
                  'SQL_Insert_1751-2000': [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
                runtimeConfiguration: {
                  concurrency: {
                    repetitions: 3
                  }
                }
              }
              'SQL_Insert_2251-2500': {
                foreach: '@take(skip(body(\'Run_script_from_SharePoint_library\')?[\'result\'], 2250), 250)'
                actions: {
                  Delay_12: {
                    runAfter: {
                      'Insert_row_(V2)_17': [
                        'Succeeded'
                      ]
                    }
                    type: 'Wait'
                    inputs: {
                      interval: {
                        count: 1
                        unit: 'Second'
                      }
                    }
                  }
                  'Insert_row_(V2)_17': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        DW_LOADED_DTTM: '@utcNow()'
                        FILE_NAME: '@triggerBody()?[\'Title\']'
                        SITE: '@coalesce(item()?[0], null)'
                        DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                        IS_DRAFT: false
                        REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                        REVENUE_CATEGORY: '@coalesce(item()?[3])'
                        VALUE_TYPE: '@coalesce(item()?[4])'
                        VALUE: '@coalesce(item()?[5],0.00)'
                      }
                      path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
                    }
                  }
                }
                runAfter: {
                  'SQL_Insert_2001-2250': [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
                runtimeConfiguration: {
                  concurrency: {
                    repetitions: 3
                  }
                }
              }
              'SQL_Insert_2501-2750': {
                foreach: '@take(skip(body(\'Run_script_from_SharePoint_library\')?[\'result\'], 2500), 250)'
                actions: {
                  Delay_5: {
                    runAfter: {
                      'Insert_row_(V2)_18': [
                        'Succeeded'
                      ]
                    }
                    type: 'Wait'
                    inputs: {
                      interval: {
                        count: 1
                        unit: 'Second'
                      }
                    }
                  }
                  'Insert_row_(V2)_18': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        DW_LOADED_DTTM: '@utcNow()'
                        FILE_NAME: '@triggerBody()?[\'Title\']'
                        SITE: '@coalesce(item()?[0], null)'
                        DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                        IS_DRAFT: false
                        REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                        REVENUE_CATEGORY: '@coalesce(item()?[3])'
                        VALUE_TYPE: '@coalesce(item()?[4])'
                        VALUE: '@coalesce(item()?[5],0.00)'
                      }
                      path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
                    }
                  }
                }
                runAfter: {
                  'SQL_Insert_2251-2500': [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
                runtimeConfiguration: {
                  concurrency: {
                    repetitions: 3
                  }
                }
              }
              'SQL_Insert_2751-3000': {
                foreach: '@take(skip(body(\'Run_script_from_SharePoint_library\')?[\'result\'], 2750), 250)'
                actions: {
                  Delay_13: {
                    runAfter: {
                      'Insert_row_(V2)_19': [
                        'Succeeded'
                      ]
                    }
                    type: 'Wait'
                    inputs: {
                      interval: {
                        count: 1
                        unit: 'Second'
                      }
                    }
                  }
                  'Insert_row_(V2)_19': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        DW_LOADED_DTTM: '@utcNow()'
                        FILE_NAME: '@triggerBody()?[\'Title\']'
                        SITE: '@coalesce(item()?[0], null)'
                        DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                        IS_DRAFT: false
                        REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                        REVENUE_CATEGORY: '@coalesce(item()?[3])'
                        VALUE_TYPE: '@coalesce(item()?[4])'
                        VALUE: '@coalesce(item()?[5],0.00)'
                      }
                      path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
                    }
                  }
                }
                runAfter: {
                  'SQL_Insert_2501-2750': [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
                runtimeConfiguration: {
                  concurrency: {
                    repetitions: 3
                  }
                }
              }
              'SQL_Insert_3001_-_3250': {
                foreach: '@take(skip(body(\'Run_script_from_SharePoint_library\')?[\'result\'], 3000), 250)'
                actions: {
                  Delay_6: {
                    runAfter: {
                      'Insert_row_(V2)_20': [
                        'Succeeded'
                      ]
                    }
                    type: 'Wait'
                    inputs: {
                      interval: {
                        count: 1
                        unit: 'Second'
                      }
                    }
                  }
                  'Insert_row_(V2)_20': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        DW_LOADED_DTTM: '@utcNow()'
                        FILE_NAME: '@triggerBody()?[\'Title\']'
                        SITE: '@coalesce(item()?[0], null)'
                        DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                        IS_DRAFT: false
                        REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                        REVENUE_CATEGORY: '@coalesce(item()?[3])'
                        VALUE_TYPE: '@coalesce(item()?[4])'
                      }
                      path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
                    }
                  }
                }
                runAfter: {
                  'SQL_Insert_2751-3000': [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
                runtimeConfiguration: {
                  concurrency: {
                    repetitions: 3
                  }
                }
              }
              'SQL_Insert_3251-3500': {
                foreach: '@take(skip(body(\'Run_script_from_SharePoint_library\')?[\'result\'], 3250), 250)'
                actions: {
                  Delay_14: {
                    runAfter: {
                      'Insert_row_(V2)_21': [
                        'Succeeded'
                      ]
                    }
                    type: 'Wait'
                    inputs: {
                      interval: {
                        count: 1
                        unit: 'Second'
                      }
                    }
                  }
                  'Insert_row_(V2)_21': {
                    type: 'ApiConnection'
                    inputs: {
                      host: {
                        connection: {
                          name: '@parameters(\'$connections\')[\'sql-1\'][\'connectionId\']'
                        }
                      }
                      method: 'post'
                      body: {
                        DW_LOADED_DTTM: '@utcNow()'
                        FILE_NAME: '@triggerBody()?[\'Title\']'
                        SITE: '@coalesce(item()?[0], null)'
                        DATE: '@formatDateTime(\r\n    addDays(\r\n        \'1899-12-30\', \r\n        if(equals(item()?[1], \'\'), 0, int(item()?[1]))\r\n    ), \r\n    \'yyyy-MM-dd\'\r\n)'
                        IS_DRAFT: false
                        REVENUE_CODE: '@coalesce(if(greaterOrEquals(length(item()?[2]), 5), substring(item()?[2], 0, 5), item()?[2])\r\n)'
                        REVENUE_CATEGORY: '@coalesce(item()?[3])'
                        VALUE_TYPE: '@coalesce(item()?[4])'
                      }
                      path: '/v2/datasets/@{encodeURIComponent(encodeURIComponent(\'default\'))},@{encodeURIComponent(encodeURIComponent(\'default\'))}/tables/@{encodeURIComponent(encodeURIComponent(\'[dbo].[REVENUE_DATAMART_DAILY_INVOICE]\'))}/items'
                    }
                  }
                }
                runAfter: {
                  'SQL_Insert_3001_-_3250': [
                    'Succeeded'
                  ]
                }
                type: 'Foreach'
                runtimeConfiguration: {
                  concurrency: {
                    repetitions: 3
                  }
                }
              }
            }
          }
          expression: {
            and: [
              {
                equals: [
                  '@parameters(\'UseStoredProc\')'
                  '@true'
                ]
              }
            ]
          }
          type: 'If'
        }
        Success_or_Failure_response_condition: {
          actions: {
            Success_Response: {
              type: 'Response'
              kind: 'Http'
              inputs: {
                statusCode: 200
                headers: {
                  'content-type': 'application/json'
                }
                body: {
                  statusCode: 200
                  body: {
                    message: 'Ready for Invoicing Logic app processed successfully'
                    logicAppRunId: '@{workflow()?[\'run\']?[\'name\']}'
                    site: '@{triggerBody()?[\'Site\']}'
                  }
                }
              }
            }
          }
          runAfter: {
            Condition_3: [
              'Succeeded'
            ]
          }
          else: {
            actions: {
              Failure_Response: {
                type: 'Response'
                kind: 'Http'
                inputs: {
                  statusCode: 500
                  headers: {
                    'content-type': 'application/json'
                  }
                  body: {
                    statusCode: 500
                    body: {
                      message: 'Ready for Invoicing Logic app failed'
                      logicAppRunId: '@{workflow()?[\'run\']?[\'name\']}'
                      site: '@{triggerBody()?[\'Site\']}'
                    }
                  }
                }
              }
            }
          }
          expression: {
            or: [
              {
                greater: [
                  '@length(body(\'List_rows_2\')?[\'value\'])'
                  0
                ]
              }
              {
                greater: [
                  '@length(body(\'List_rows_3\')?[\'value\'])'
                  0
                ]
              }
            ]
          }
          type: 'If'
        }
      }
      outputs: {}
    }
    parameters: {
      '$connections': {
        value: {
          'commondataservice-1': {
            id: '/subscriptions/72bb1233-bb68-442b-85f3-ef6bf21a6216/providers/Microsoft.Web/locations/eastus2/managedApis/commondataservice'
            connectionId: connections_commondataservice_1_externalid
            connectionName: 'commondataservice-2'
          }
          office365: {
            id: '/subscriptions/72bb1233-bb68-442b-85f3-ef6bf21a6216/providers/Microsoft.Web/locations/eastus2/managedApis/office365'
            connectionId: connections_office365_externalid
            connectionName: 'office365'
          }
          sharepointonline: {
            id: '/subscriptions/72bb1233-bb68-442b-85f3-ef6bf21a6216/providers/Microsoft.Web/locations/eastus2/managedApis/sharepointonline'
            connectionId: connections_sharepointonline_externalid
            connectionName: 'sharepointonline'
          }
          excelonlinebusiness: {
            id: '/subscriptions/72bb1233-bb68-442b-85f3-ef6bf21a6216/providers/Microsoft.Web/locations/eastus2/managedApis/excelonlinebusiness'
            connectionId: connections_excelonlinebusiness_externalid
            connectionName: 'excelonlinebusiness'
          }
          'sql-1': {
            id: '/subscriptions/72bb1233-bb68-442b-85f3-ef6bf21a6216/providers/Microsoft.Web/locations/eastus2/managedApis/sql'
            connectionId: connections_sql_1_externalid
            connectionName: 'sql-1'
          }
        }
      }
    }
  }
}
