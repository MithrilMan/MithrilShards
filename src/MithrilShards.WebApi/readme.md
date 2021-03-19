<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
**Table of Contents**

- [Note about swagger documentation](#note-about-swagger-documentation)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

﻿Contains classes to implement API features.

To be able to use APIs, API feature has to be enabled with .UseApi

Endpoint aren't specified there because are injected by the shard itself implementing the interface IApiEndpoint



### Note about swagger documentation

In order to see comments on swagger ui, you just need to comment your controllers and generate XML documentation during the build process of the assembly containing these controllers.

The easier way is to edit your project file adding this snippet

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```