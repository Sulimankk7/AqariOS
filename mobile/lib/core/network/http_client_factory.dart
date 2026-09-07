import 'package:http/http.dart' as http;

import 'http_client_factory_native.dart'
    if (dart.library.html) 'http_client_factory_web.dart'
    as platform;

http.Client createAqariHttpClient() => platform.createAqariHttpClient();

bool get usesBrowserCookieJar => platform.usesBrowserCookieJar;
