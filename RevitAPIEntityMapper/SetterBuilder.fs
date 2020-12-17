module SetterBuilder
open TypeResolver
open Visitor
open ExprHelpers
open ExpressionVisitor
open System.Collections.Generic
open System
open Autodesk.Revit.DB.ExtensibleStorage
open Autodesk.Revit.DB
open FSharp.Quotations

let miSet = 
    let e = Entity ()
    <@ e.Set("",obj()) @> |> genericMethodInfo

let miutSet = 
    let e = Entity ()
    <@ e.Set("",obj(),DisplayUnitType.DUT_1_RATIO) @> |> genericMethodInfo

let miReferenceEqual =
    <@ obj.ReferenceEquals(null,null) @> |> getMethodInfo

let setterInit (factories:Dictionary<Type,obj>) def =
    match factories.TryGetValue def.entityType with
    |(true,factory) -> factory |> Complited
    |(false,_) -> let obj = Var ("obj", def.entityType)
                  let ent = Var ("e",typeofEntity)
                  let objExpr = Expr.Var obj
                  let newObj = <@ Entity(%(Val def.guid |> Expr.Cast<Guid>))@>
                  let nullCondititon = Call miReferenceEqual >> With [objExpr; Val null] <| ()
                  let thenStatement = <@ Entity() @>
                  let elseStatement body = Expr.Let(ent,newObj,body)
                  let lmd body = Expr.Lambda(obj,If nullCondititon >> Then thenStatement >> Else (elseStatement body) <| ()) 
                  let ctx = { 
                    input = objExpr
                    bindings = []
                    lambdaExpr = lmd
                    output = Expr.Var ent
                    defaultUT = readUnit def.entityType
                  }
                  NeedsCreate(ctx)

let fetch ctx _ =
    Expr.PropertyGet(ctx.stepState.input ,ctx.info)

let response ctx (body:Expr) = 
    let nullCondition = Call miReferenceEqual >> With [body ;Val null] <| ()

    let ifNotNull expr = 
        match ctx.info.PropertyType.IsValueType with
         |true -> expr
         |false -> If nullCondition >> Then (Val ()) >> Else expr <| ()

    fetchUnitType 
     (fun optUT ->
        match optUT with
        |Some(ut) -> Call miutSet >> MakeGen[body.Type] >> On ctx.stepState.output >> With [Val ctx.info.Name; body; Val ut] <| () 
                     |> ifNotNull |> createNewCtx ctx.stepState
        |Option.None -> Call miSet >> MakeGen[body.Type] >> On ctx.stepState.output >> With [Val ctx.info.Name; body] <| ()
                        |> ifNotNull |> createNewCtx ctx.stepState)
     ctx

let setterBody = exprBody fetch response (fun def -> (def.entityType,typeofEntity)) 

let setterBuilder factories = 
    visitorBuilder (setterInit factories) setterBody (fun ctx -> let factory = finallize ctx
                                                                 factories.Add(ctx.input.Type,factory)
                                                                 factory)
