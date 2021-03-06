# Contributing to rebar 

Contributions to rebar are welcome from all!

rebar is managed via [git](https://git-scm.com), with the canonical upstream
repository hosted on [GitHub](https://github.com/ni/rebar/).

rebar follows a pull-request model for development.  If you wish to
contribute, you will need to create a GitHub account, fork this project, push a
branch with your changes to your project, and then submit a pull request.

See [GitHub's official documentation](https://help.github.com/articles/using-pull-requests/) for more details.

# Getting Started

Builds with Visual Studio 2015 against .NET Framework 4.6.2.

You will need [LabVIEW NXG 3.0](http://www.ni.com/nl-nl/support/downloads/software-products/download.labview-nxg.html) installed in order
to build and run the Rebar addon. Once it is installed, copy the InstallLocation.targets file from the repository top level to the Rebar
folder, and update the InstallLocation value with the path to your LabVIEW NXG 3.0 installation. Rebar builds to the 
$(InstallLocation)\Addons\rb\Rebar folder.

# Testing

Automated tests are TODO.

# Developer Certificate of Origin (DCO)

   Developer's Certificate of Origin 1.1

   By making a contribution to this project, I certify that:

   (a) The contribution was created in whole or in part by me and I
       have the right to submit it under the open source license
       indicated in the file; or

   (b) The contribution is based upon previous work that, to the best
       of my knowledge, is covered under an appropriate open source
       license and I have the right under that license to submit that
       work with modifications, whether created in whole or in part
       by me, under the same open source license (unless I am
       permitted to submit under a different license), as indicated
       in the file; or

   (c) The contribution was provided directly to me by some other
       person who certified (a), (b) or (c) and I have not modified
       it.

   (d) I understand and agree that this project and the contribution
       are public and that a record of the contribution (including all
       personal information I submit with it, including my sign-off) is
       maintained indefinitely and may be redistributed consistent with
       this project or the open source license(s) involved.

(taken from [developercertificate.org](https://developercertificate.org/))

See [LICENSE](https://github.com/ni/rebar/blob/master/LICENSE)
for details about how rebar is licensed.
