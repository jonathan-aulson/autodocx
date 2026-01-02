# PowerServer-Console-PB-Example

This demo is a PowerBuilder application, developed with Appeon [PowerBuilder 2025](https://www.appeon.com/products/powerbuilder). It shows: 

1. Displaying the current PowerServer license and instance information.
2. Configuration management including the management of applications and connections.
3. Monitoring the running status of the current PowerServer including the information of sessions and transactions.


Note: 

### Sample Project Structure

The project is structured as follows.

```
|—— PowerServer-Console-PB-Example Repository 
	|—— appeon.PowerServerConsoleDemo.Expanded				PowerBuilder project source code
```

### Setting Up the Project

Download this PowerBuilder demo application, and then:

1. Open the PowerBuilder application in PowerBuilder 2025.

2. In the PowerServerConsole.ini file, configure your PowerSever API Server URL.

```bash
[Setting]
APIHost = https://demo.appeon.com:443/api.sales
EnableAuthentication= false

Note: If your PowerSever API Server uses Auth Service, please set EnableAuthentication = True in PowerServerConsole.ini and
      modify the f_Authorization() code in the application to be consistent with your PowerServer API Server.
```
3. Run the PowerBuilder application.

4. You can then monitor and manage your PowerServer API Server using this console.


### PowerServer Deployment

The source code includes a PowerServer project. If you want to deploy it using PowerServer, you need to go to the project object, switch to the Web APIs tab, and import your own license and then you can directly deploy it. (If you current PowerBuilder login account has PowerServer license, you can choose Auto Import, otherwise, you can choose Import from file.)
