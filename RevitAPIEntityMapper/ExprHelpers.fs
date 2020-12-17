module ExprHelpers
open System.Reflection
open FSharp.Quotations

type 'info ExprContext = {
    obj: Option<Expr>
    memberInfo: 'info
    }

let Call (mi:MethodInfo) () =
    {memberInfo = mi;obj = None}

let On obj ctx =
    {ctx with obj = Some(obj)}

let With args (ctx:ExprContext<MethodInfo>) = 
    match ctx.obj with
        |Some(o) -> Expr.Call(o,ctx.memberInfo,args)
        |None -> Expr.Call(ctx.memberInfo,args)

let WithConst args ctx =
    let values = args |> List.map (fun v->Expr.Value(v))
    With values ctx

let MakeGen types (ctx:ExprContext<MethodInfo>) =
    let typesArr = types|>List.toArray
    match ctx.memberInfo.IsGenericMethodDefinition with
        |true ->let gDef = ctx.memberInfo.MakeGenericMethod typesArr
                {ctx with memberInfo = gDef}
        |false -> let def= ctx.memberInfo.GetGenericMethodDefinition ()
                  let gDef = def.MakeGenericMethod typesArr
                  {ctx with memberInfo  = gDef}

let SetProp (pi:PropertyInfo) () =
    {memberInfo= pi;obj=None}


let To value (ctx:ExprContext<PropertyInfo>) = 
    match ctx.obj with
        |Some(o) -> Expr.PropertySet(o,ctx.memberInfo,value)
        |None -> Expr.PropertySet(ctx.memberInfo,value)

let Val constant = Expr.Value constant

let Cast t expr = Expr.Coerce(expr,t)

let If expr ()= 
    (fun thenStatement elseStatement -> Expr.IfThenElse(expr,thenStatement,elseStatement))

let Then expr ifStatement = 
    ifStatement expr
    
let Else expr ifThenStatement =
    ifThenStatement expr
