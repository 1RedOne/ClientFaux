![ClientFaux logo, think 'sly like a fox'](https://github.com/1RedOne/ClientFaux/raw/master/ClientFaux/Images/ClientFaux3.0.png)

### Instructions

**Requires HTTP Mode enabled on Primary**
*Working on allowing https mode, but presently only expected to work with a primary which allows http communications*

Navigate to the `⚙CM Settings' tab and provide your CM Servers FQDN or netbios name, then the three letter site code.  The tool **will not** work without both.

![Filling in Site Info](https://github.com/1RedOne/ClientFaux/raw/master/ClientFaux/Images/docs_01_fcmsettings.png)

Then switch to the **Naming** tab and provide your client base name and the starting and ending device IDs you'd like to create in CM.

![Filling in Device Info](https://github.com/1RedOne/ClientFaux/raw/master/ClientFaux/Images/docs_02_settings02.png)

Then hit 'Ready' from the main tab when you're ready to go!

*These settings are now saved between launches of the application!*

The client should appear within CM in ten seconds or so!  Hardware Inventory will take a bit longer to appear.

### Providing Custom Inventory

//todo...


### Reconciliation and Reporting

All devices created with ClientFaux recieve the custom DDR property of `ClientType : FakeClient`, you can use this property to exclude all fake Clients, or to exclude them instead in your reports and Collections.

### Troubleshooting 

Check `DDM.Log` and `mpcontrol.log` on the ConfigMgr primary site for troubleshooting information and also the `\SMS_CCM\Logs\MP_RegistrationManager.log` file for additional troubleshooting.  Now also creates client side logging as well, which will be found in the working directory of the app, as `DebugCMLog.log`