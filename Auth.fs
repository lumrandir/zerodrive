module Auth

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Text.RegularExpressions
open System.Web

let (|IniValue|_|) pattern input =
  let m = Regex.Match(input, "^" + pattern + "=(.+)$")
  if (m.Success) then Some m.Groups.[1].Value else None

type AuthConfig =
  { clientId     : string
    responseType : string
    redirectUri  : string
    scope        : string
  }

  member this.urlenc : string =
    [ "client_id=" + WebUtility.UrlEncode(this.clientId)
    ; "response_type=" + WebUtility.UrlEncode(this.responseType)
    ; "redirect_uri=" + WebUtility.UrlEncode(this.redirectUri)
    ; "scope=" + WebUtility.UrlEncode(this.scope)
    ] |> String.concat "&"

  static member defaultConfig : AuthConfig =
    { clientId = ""
    ; responseType = "code"
    ; redirectUri = "http://localhost:8080"
    ; scope = "offline_access onedrive.readwrite"
    }

  static member load (path : string) : AuthConfig =
    let ini = File.ReadLines(path)
    let clientId =
      Seq.pick (fun line ->
                  match line with
                  | IniValue "client_id" value -> Some value
                  | _ -> None
      ) ini

    { AuthConfig.defaultConfig with clientId = clientId }

let private acceptClient (client : TcpClient) handler =
  use stream = client.GetStream()
  use reader = new StreamReader(stream)
  let header = reader.ReadLine()
  if not (String.IsNullOrEmpty(header)) then
    use writer = new StreamWriter(stream)
    let result = handler (header, writer)
    writer.Flush()
    result
  else
    ""

let private authServerUrl =
  "https://login.live.com/oauth20_authorize.srf"

let private authUrl (config : AuthConfig) : string =
  authServerUrl + "?" + config.urlenc

let private printAuthUrl (config : AuthConfig) : unit =
  printfn "Get the code here: %s" (authUrl config)

let private onCallback codeFilePath (header : string, writer : IO.StreamWriter) =
  let split = header.Split(" ")
  let path = split.[1]
  let code = HttpUtility.ParseQueryString(path).Item(0)
  writer.Write("HTTP/1.1 200 OK\r\n\r\n")
  writer.Write("Got it.")
  File.WriteAllText(codeFilePath, code)
  code

let private startCallbackServer (addr : string, port : int) handler =
  let ip = IPAddress.Parse(addr)
  let listener = TcpListener(ip, port)
  listener.Start()
  let client = listener.AcceptTcpClient()
  acceptClient client handler

let private requestAuthCode home handler =
  let path = home + "/.zerodrive/zerodriverc"
  if not <| File.Exists(path) then
    printfn "Configuration file not found!"
    exit(1)
  else
    let config = AuthConfig.load(path)
    printAuthUrl config
    startCallbackServer ("127.0.0.1", 8080) handler

let authenticate =
  let home = Environment.GetEnvironmentVariable("HOME")
  let codeFilePath = home + "/.zerodrive/code"
  if not <| File.Exists(codeFilePath) then
    requestAuthCode home (onCallback codeFilePath)
  else
    File.ReadAllText(codeFilePath)
