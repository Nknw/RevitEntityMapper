open FSharp.Quotations
open FSharp.Quotations.Evaluator
open System.Reflection
open System.Collections.Generic

let getMethodInfo (e : Expr<'T>) : MethodInfo =
    match e with
        | Patterns.Call (_, mi, _) -> mi
        | _ -> failwithf "Expression has the wrong shape, expected Call (_, _, _) instead got: %A" e

let genericMethodInfo (e : Expr<'T>) : MethodInfo =let typedInfo = getMethodInfo e
                                                   typedInfo.GetGenericMethodDefinition ()



let quote arr = 
    <@  fun () -> let b =Seq.map (fun i->i+1) %%arr 
                  Seq.map (fun k->k+2) b@>

[<EntryPoint>]
let main argv =
    let lmd = quote (Expr.Value(seq{1}))
    let res = lmd.CompileUntyped() :?> unit->seq<int>
    printfn "%A" (res())
    0