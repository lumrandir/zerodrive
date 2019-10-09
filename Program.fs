// Learn more about F# at http://fsharp.org

open Auth
open System

[<EntryPoint>]
let main argv =
  let code = authenticate
  printfn "code: %s" code
  0
