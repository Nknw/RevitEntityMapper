module SetterBuilder
open Abstractions
open ExprHelpers
open System.Collections.Generic
open System
open Autodesk.Revit.DB.ExtensibleStorage
open FSharp.Quotations

let setterInt (factories:Dictionary<Type,obj>) def =
    match factories.TryGetValue def.entityType with
    |(true,factory) -> factory |> Success |> Complited
    |(false,_) -> let t = typeof<Entity>
                  let constructor = [typeof<Guid>] |> List.toArray |> t.GetConstructor
                  let obj = Var ("obj", def.entityType)
                  let ent = Var ("e",typeofEntity)
                  let newObj = (constructor,[Val def.guid]) |> Expr.NewObject
                  let lmd body = Expr.Lambda(obj,Expr.Let(ent,newObj,body)) 
                  let ctx = { 
                    input = Expr.Var obj
                    bindings = []
                    lambdaExpr = lmd
                    output = Expr.Var ent
                    defaultUT = Option.None //readMeta entity.entityType
                  }
                  NeedsCreate(ctx)
