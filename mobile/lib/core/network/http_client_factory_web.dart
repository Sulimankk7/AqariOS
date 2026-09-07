import 'package:http/browser_client.dart';
import 'package:http/http.dart' as http;

http.Client createAqariHttpClient() => BrowserClient()..withCredentials = true;

const usesBrowserCookieJar = true;
