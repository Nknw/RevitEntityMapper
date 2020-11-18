open FSharp.Quotations
open FSharp.Quotations.Evaluator
open System.Reflection
open System.Collections.Generic
open System.Linq.Expressions
open System.Collections
open FSharp.Linq.RuntimeHelpers

let getMethodInfo (e : Expr<'T>) : MethodInfo =
    match e with
        | Patterns.Call (_, mi, _) -> mi
        | _ -> failwithf "Expression has the wrong shape, expected Call (_, _, _) instead got: %A" e

let genericMethodInfo (e : Expr<'T>) : MethodInfo =let typedInfo = getMethodInfo e
                                                   typedInfo.GetGenericMethodDefinition ()

let map = <@Seq.map (fun f->f) [""]@> |> genericMethodInfo

let se = seq{1}
let cast = <@Seq.cast<obj> (Seq.map (fun i-> "") se) @> |> genericMethodInfo

let tp = [typeof<int>]|>List.toArray


let c = Expr.Value(b)
let b (i:int) = 1 
let pipe = <@ 1|>b @> |> genericMethodInfo
let gpipe = pipe.MakeGenericMethod ([typeof<int>;typeof<int>]|>List.toArray)
let ind = Var("e",typeof<unit>)
let lmd = Expr.Lambda(ind,Expr.Call(gpipe,[Expr.Value 1;Expr.Value b]))
let lamda = LeafExpressionConverter.EvaluateQuotation lmd :?> unit-> int
printfn "%A" (lamda())

let e = Var ("e",typeof<List<string>>)
let l = Expr.Lambda(e,Expr.Call(map.MakeGenericMethod ([typeof<string>;typeof<string>]|>List.toArray),[Expr.Value(fun (s:string)->s);Expr.Var e]))

let dicte = Dictionary<string,string>()
let Expr = <@ dicte.Values@>

[<EntryPoint>]
let main argv =
    printfn "%A" l
    let f = LeafExpressionConverter.EvaluateQuotation l
    let lm = f :?> List<string>->string seq
    printfn "%A" (lm (List<string>()))
    0