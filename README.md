![ClientFaux logo, think 'sly like a fox'](https://foxdeploy.files.wordpress.com/2018/06/clientfaux-1.png)

# AddCMClients
A tool for adding fake clients to ConfigMgr for collection querying, testing and reporting

### Setting up the environment

This tool depends on you having the [ConfigMgr SDK](https://www.microsoft.com/en-us/download/details.aspx?id=29559) Available on your machine.  It currently expects the following .dll file to be present in its root folder.

1. Run the installer above, accepting and noting the license terms
2. Copy the following Microsoft.ConfigurationManagement.Messaging.dll file to the same location where you've placed the binary for this project.  Default location for the needed DLL file is `C:\Program Files (x86)\Microsoft System Center 2012 R2 Configuration Manager SDK\Redistributables\Microsoft.ConfigurationManagement.Messaging.dll`  
3. (e.g. If you place these files under C:\Git\AddCMClients, then place the .dll in that location as well)
4. Ensure your ConfigMgr instance is configured to 'Approve all Clients' 

Now you're ready to use AddCMClients!



#### What's here

| Version  |  Feature | 
|---|---|
| No more hardcoded variables!  | [v0.1-alpha](https://github.com/1RedOne/ClientFaux/releases/tag/alpha) |
| Flexible Cert Path  | next  |
|  WPF GUI | v2.0  |
| PowerShell Cmdlet? | v3.0|

*tested on ConfigrMgr Current Branch v1802*
