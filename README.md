![ClientFaux logo, think 'sly like a fox'](https://github.com/1RedOne/ClientFaux/blob/master/ClientFaux/Images/CMFoxv1.0Logo.png)

# ClientFaux - Client Simulation Tool
A tool for adding simulated CM Clients to ConfigMgr for collection querying, report building, and testing ConfigMgr environments.  [Read the blog post here.](https://foxdeploy.com/2018/06/08/how-to-populate-cm-with-fake-clients/)
<a class="btn btn-sm new-pull-request-btn" href="https://github.com/1RedOne/ClientFaux/releases">Download now! 🦊</a>
### Setting up the environment

There is now practically no setup to the tool, simply download the newest release from releases above!  Run the installer then launch ClientFaux as a standard user account (or admin, doesn't matter).

Navigate to the `⚙CM Settings' tab and provide your CM Servers FQDN or netbios name, then the three letter site code.  The tool **will not** work without both.

Then switch to the **Naming** tab and provide your client base name and the starting and ending device IDs you'd like to create in CM.

Then hit 'Ready' from the main tab when you're ready to go!

The client should appear within CM in ten seconds or so!  Hardware Inventory will take a bit longer to appear.


#### What's here

| Feature  |  Version | Done? |
|---|---|---|
| No more hardcoded variables!  | [v0.1-alpha](https://github.com/1RedOne/ClientFaux/releases/tag/alpha) |✔️|
| No more hardcoded paths  | [v0.2-alpha](https://github.com/1RedOne/ClientFaux/releases/tag/alpha)  |✔️|
| Flexible Cert Path  | [v0.2-alpha](https://github.com/1RedOne/ClientFaux/releases/tag/alpha)  |✔️|
| Better logging | [v0.3-alpha](https://github.com/1RedOne/ClientFaux/releases/tag/alpha)  |✔️|
| Working Client Inventory | [v1.0](https://github.com/1RedOne/ClientFaux/releases/tag/v1.0) | ✔️|
| WPF GUI | v2.0  | ✔️ |
| User Custom Client Discovery Values |v 2.1| ---|
| Machine and User policy download |v 2.2| ---|
| Machine and User policy viewing |v 2.x| ---|
| Client HeartBeat Sending |v 2.x | ---|
| Support for compression  |v 2.x| ---|
| Overload individual inventory items |v 3.x | ---|
| Maintain localdb for future reuse of clients |v 3.x | ---|

Custom DDM now works too!

![ClientFaux logo, think 'sly like a fox'](https://i.imgur.com/8D3xUgW.png)

# Warning
This is meant for TestLab use only.  Proceed with caution

*tested on ConfigrMgr Current Branch v1810*
