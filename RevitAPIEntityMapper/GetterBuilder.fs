module GetterBuilder
open TypeResolver
open Visitor
open ExprHelpers
open ExpressionVisitor
open System
open System.Collections.Generic
open FSharp.Quotations
open Autodesk.Revit.DB.ExtensibleStorage
open Autodesk.Revit.DB

let miGet = 
    let e = Entity ()
    <@ e.Get "" @> |> genericMethodInfo

let miutGet =
    let e = Entity()
    <@ e.Get ("", DisplayUnitType.DUT_1_RATIO) @> |> genericMethodInfo

let miIsValid = 
    let e = Entity()
    <@ e.IsValid () @> |> getMethodInfo
   
let getterInit (factories:Dictionary<Type,obj>) entity = 
    match factories.TryGetValue entity.entityType with
    |(true,factory) -> factory |> Complited
    |(false,_) -> let t = entity.entityType
                  let obj = Var ("obj", t)
                  let ent = Var ("e",typeofEntity)
                  let entExpr = Expr.Var ent
                  let newObj = Expr.DefaultValue t
                  let validate = Call miIsValid >> On entExpr >> With [] <| ()
                  let thenStatement body = Expr.Let(obj,newObj,body)
                  let elseStatement = Val null |> Cast t 
                  let lmd body = Expr.Lambda(ent,If validate >> Then (thenStatement body) >> Else elseStatement <|()) 
                  let ctx = { 
                    input = entExpr
                    bindings = []
                    lambdaExpr = lmd
                    output = Expr.Var obj
                    defaultUT = readUnit entity.entityType
                  }
                  NeedsCreate(ctx)

let response ctx body = 
    let state = ctx.stepState
    SetProp (ctx.info) >> On (state.output) >> To body <| () |> createNewCtx state

let getterBody = 
    let fetch ctx t = 
        fetchUnitType
         (fun optUT-> 
            match optUT with
            |Some(ut) -> Call miutGet >> MakeGen [t] >> On ctx.stepState.input >> With [Val ctx.info.Name;Val ut] <| ()
            |None -> Call miGet >> MakeGen [t] >> On ctx.stepState.input >> WithConst [ctx.info.Name] <| ())
         ctx
    exprBody fetch response (fun def -> (typeofEntity,def.entityType))

let getterBuilder factories = 
    visitorBuilder (getterInit factories) getterBody (fun ctx ->let factory =  finallize ctx
                                                                factories.Add(ctx.output.Type,factory)
                                                                factory)
