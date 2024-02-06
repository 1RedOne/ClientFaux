![ClientFaux logo, think 'sly like a fox'](https://raw.githubusercontent.com/1RedOne/ClientFaux/master/Images/ClientFaux3.0.jpg)

# ClientFaux - Client Simulation Tool
A tool for adding simulated CM Clients to ConfigMgr for collection querying, report building, and testing ConfigMgr environments.  [Read the blog post here.](https://foxdeploy.com/2018/06/08/how-to-populate-cm-with-fake-clients/)

[![start with why](https://img.shields.io/badge/Want%20to%20try%3F-Download%20Now!-brightgreen.svg?style=flat)](https://github.com/1RedOne/ClientFaux/releases)

### Setting up the environment

**Requires HTTP Mode enabled on Primary**
*Working on allowing https mode, but presently only expected to work with a primary which allows http communications*

There is now practically no setup to the tool, simply download the newest release from releases above!  Run the installer then launch ClientFaux as a standard user account (or admin, doesn't matter).

Navigate to the `⚙CM Settings' tab and provide your CM Servers FQDN or netbios name, then the three letter site code.  The tool **will not** work without both.

Then switch to the **Naming** tab and provide your client base name and the starting and ending device IDs you'd like to create in CM.

Then hit 'Ready' from the main tab when you're ready to go!

The client should appear within CM in ten seconds or so!  Hardware Inventory will take a bit longer to appear.


#### Feature Roadmap

| Feature  |  Version | Done? |
|---|---|---|
| No more hardcoded variables!  | [v0.1-alpha](https://github.com/1RedOne/ClientFaux/releases/tag/alpha) |✔️|
| No more hardcoded paths  | [v0.2-alpha](https://github.com/1RedOne/ClientFaux/releases/tag/alpha)  |✔️|
| Flexible Cert Path  | [v0.2-alpha](https://github.com/1RedOne/ClientFaux/releases/tag/alpha)  |✔️|
| Better logging | [v0.3-alpha](https://github.com/1RedOne/ClientFaux/releases/tag/alpha)  |✔️|
| Working Client Inventory | [v1.0](https://github.com/1RedOne/ClientFaux/releases/tag/v1.0) | ✔️|
| WPF GUI | v2.0  | ✔️ |
| User Custom Client Discovery Values |[v2.1](https://github.com/1RedOne/ClientFaux/releases/tag/v2.1.0)| ✔️|
| Support for compression  |v 2.2 | ✔️|
| Logging |[v3.0](https://github.com/1RedOne/ClientFaux/releases/tag/v3.0)| ✔️|
| Integrated Installer |[v3.0](https://github.com/1RedOne/ClientFaux/releases/tag/v3.0)| ✔️|
| Save Settings |v3.1 | --- ||
| Custom Inventory of apps | --- | ---|
| Machine and User policy viewing  | --- | ---|
| Client HeartBeat Sending | --- | ---|
| Reusable Clients   | --- | ---|

Custom DDM now works too!

![ClientFaux logo, think 'sly like a fox'](https://i.imgur.com/8D3xUgW.png)

### Reconciliation and Reporting

All devices created with ClientFaux recieve the custom DDR property of `ClientType : FakeClient`, you can use this property to exclude all fake Clients, or to exclude them instead in your reports and Collections.

### Troubleshooting 

Check `DDM.Log` and `mpcontrol.log` on the ConfigMgr primary site for troubleshooting information and also the `\SMS_CCM\Logs\MP_RegistrationManager.log` file for additional troubleshooting.  Now also creates client side logging as well, which will be found in the working directory of the app, as `DebugCMLog.log`

# Warning
This is meant for TestLab use only.  Proceed with caution

*tested on ConfigrMgr Current Branch v2002*
