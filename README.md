# Dragonfly.Umbraco8FormsMembers #

Umbraco 8 Forms additions to work with Members created by [Heather Floyd](https://www.HeatherFloyd.com).

With special thanks to [Tim Geyssens](https://github.com/TimGeyssens/MemberToolsForUmbracoForms.git) for original v7 code. 


**Notes:**
For Your Registration Form Password field, be sure to set a proper Validation rule, based on what your Web.config has for "UmbracoMembershipProvider" rules (minRequiredPasswordLength & minRequiredNonalphanumericCharacters). For example:
 
minRequiredPasswordLength = 10
minRequiredNonalphanumericCharacters = 0

RegEx: `\w{10,}`
Message: Passwords must be at least 10 characters

For the POST Url - it needs to be publicly accessible since when a form is submitted there is no authenticated Umbraco User.