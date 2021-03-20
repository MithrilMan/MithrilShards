<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
**Table of Contents**

- [If you have problem serving embedded resources](#if-you-have-problem-serving-embedded-resources)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## If you have problem serving embedded resources

When using embedded resources, remember that some characters are transformed.

e.g. if you create a folder with a hypens, like `swagger-ui` it gets transformed to `swagger_ui`

For your *mental health* I **suggest** you to just use a-z A-Z 0-9 and _ in your embedded resource folders and files.