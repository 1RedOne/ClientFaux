![ClientFaux logo, think 'sly like a fox'](https://foxdeploy.files.wordpress.com/2018/06/clientfaux-1.png)

# ClientFaux - Client Simulation Tool
A tool for adding simulated CM Clients to ConfigMgr for collection querying, report building, and testing ConfigMgr environments.  [Read the blog post here.](https://foxdeploy.com/2018/06/08/how-to-populate-cm-with-fake-clients/)

### Setting up the environment

This tool depends on you having the [ConfigMgr SDK](https://www.microsoft.com/en-us/download/details.aspx?id=29559) Available on your machine.  It currently expects the following .dll file to be present in its root folder.

1. Run the installer above, accepting and noting the license terms
2. Copy the following Microsoft.ConfigurationManagement.Messaging.dll file to the same location where you've placed the binary for this project.  Default location for the needed DLL file is `C:\Program Files (x86)\Microsoft System Center 2012 R2 Configuration Manager SDK\Redistributables\Microsoft.ConfigurationManagement.Messaging.dll`  
3. (e.g. If you place these files under C:\Git\AddCMClients, then place the .dll in that location as well)
4. Ensure your ConfigMgr instance is configured to 'Approve all Clients' 
5. Create a Self-Signed cert by running PowerShell as admin and running the following

````powershell

$newCert = New-SelfSignedCertificate `
    -KeyLength 2048 `
    -HashAlgorithm "SHA256" `
    -Provider  "Microsoft Enhanced RSA and AES Cryptographic Provider" `
    -KeyExportPolicy Exportable -KeySpec KeyExchange `
    -Subject 'SCCM Test Certificate' -KeyUsageProperty All -Verbose 
    
    start-sleep -Milliseconds 650

    $pwd = ConvertTo-SecureString -String 'Pa$$w0rd!' -Force -AsPlainText

Export-PfxCertificate -cert cert:\localMachine\my\$($newCert.Thumbprint) -FilePath c:\temp\MixedModeTestCert.pfx -Password $pwd -Verbose
````
5. Run the binary with the following syntax

````powershell
ClientFaux <DeviceName> <CertPath> <CertPW> <SiteCode> <Management point name>
````

#### Example

If you wanted to create a new fake device in CM called 'FoxPC123', you exported the Cert to C:\temp\MixedModeTestCert.pfx, with the same password above, and you have a SiteCode of F0X and a MP of SCCM, you'd run:

    ClientFaux FoxPC123 c:\temp\MixedModeTestCert.pfx 'Pa$$w0rd!' F0X SCCM

You should see the following output

````

Logging to: C:\temp\AddClients\bin\sXS\3\ClientFauxLogs.txt
Found cert file at c:\temp\MixedModeTestCert.pfx
Running on system[DC2016], registering as [FoxPC123]
Using certificate for client authentication with thumbprint of '0857221E15006E9B5980AFCBF127CD87E740AB54'
Signature Algorithm: sha256RSA
Cert has a valid sha256RSA Signature Algorithm, proceeding
Trying to reach: SCCM.FoxDeploy.local
About to try to register FoxPC123.FoxDeploy.local
Got SMSID from registration of: GUID:5A06B3E8-622C-472C-ACFC-C1D9F962128D
ddrSettings clientID: GUID:5A06B3E8-622C-472C-ACFC-C1D9F962128D
ddrSettings SiteCode: F0X
ddrSettings ADSiteNa: Default-First-Site-Name
ddrSettings DomainNa: FoxDeploy.local
ddrSettings FakeName: FoxPC123
Message MPHostName  : SCCM.FoxDeploy.local
Sending: 104instances of HWinv data to CM

````

The client should appear within CM in ten seconds or so!


#### What's here

| Feature  |  Version | Done? |
|---|---|---|
| No more hardcoded variables!  | [v0.1-alpha](https://github.com/1RedOne/ClientFaux/releases/tag/alpha) |✔️|
| No more hardcoded paths  | [v0.2-alpha](https://github.com/1RedOne/ClientFaux/releases/tag/alpha)  |✔️|
| Flexible Cert Path  | [v0.2-alpha](https://github.com/1RedOne/ClientFaux/releases/tag/alpha)  |✔️|
| Better logging | [v0.3-alpha](https://github.com/1RedOne/ClientFaux/releases/tag/alpha)  |✔️|
| Better Cert Creation Script | --- |---|
|  WPF GUI | v2.0  |---|
| Working Client Inventory | ---| ---|
| Working Discovery |--- |---|
| PowerShell Cmdlet? | v3.0|---|

In the current version of the tool, we enroll a device and discard the certificate.  In a future version of the tool, we will try to maintain the certificate to use with subsequent communications, or to simulate recieving Windows Updates, etc.  The sky is the limit!  

# Warning
This is meant for TestLab use only.  Proceed with caution

*tested on ConfigrMgr Current Branch v1802*
