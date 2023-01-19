# u-blox u-connect echo-test

Test application to provide echo functionality to a u-blox u-connectXpress EVK. i.e. ANNA/NINA

The u-blox 's-center' application does not seem to provide a way to directly echo data from Android device over SPS back once recieved by the EVK.

This application is primarily to allow testing different Android devices stability when higher MTUs are requested, as Samsung in particular seems to have issues when using something other than an MTU of 23.

See also app side [https://github.com/ajchellew/Android-u-blox-BLE](https://github.com/ajchellew/Android-u-blox-BLE)

References: 

- [u-connectXpress User Guide](https://content.u-blox.com/sites/default/files/u-connectXpress_UserGuide_UBX-16024251.pdf)
- [u-connectXpress ATCommands Manual](https://content.u-blox.com/sites/default/files/u-connectXpress-ATCommands-Manual_UBX-14044127.pdf)
