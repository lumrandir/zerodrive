module Auth

open System.IO
open System.Net
open System.Text.RegularExpressions

let (|IniValue|_|) pattern input =
  let m = Regex.Match(input, "^" + pattern + "=(.+)$")
  if (m.Success) then Some m.Groups.[1].Value else None

type AuthConfig =
  { client_id     : string
    response_type : string
    redirect_uri  : string
    scope         : string
  }

  member this.urlenc : string =
    [ "client_id=" + WebUtility.UrlEncode(this.client_id)
    ; "response_type=" + WebUtility.UrlEncode(this.response_type)
    ; "redirect_uri=" + WebUtility.UrlEncode(this.redirect_uri)
    ; "scope=" + WebUtility.UrlEncode(this.scope)
    ] |> String.concat "&"

  static member default_config : AuthConfig =
    { client_id = ""
    ; response_type = "code"
    ; redirect_uri = "http://localhost:8080"
    ; scope = "offline_access onedrive.readwrite"
    }

  static member load (path : string) : AuthConfig =
    let ini = File.ReadLines(path)
    let client_id =
      Seq.pick (fun line ->
                  match line with
                  | IniValue "client_id" value -> Some value
                  | _ -> None
      ) ini

    { AuthConfig.default_config with client_id = client_id }


let private auth_server_url : string =
  "https://login.live.com/oauth20_authorize.srf"

let private auth_url (config : AuthConfig) : string =
  auth_server_url + "?" + config.urlenc

let print_auth_url (config : AuthConfig) : unit =
  printfn "Get the code here: %s" (auth_url config)

