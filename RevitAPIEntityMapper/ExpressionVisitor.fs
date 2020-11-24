module ExpressionVisitor
open TypeResolver
open ExprHelpers
open Visitor
open FSharp.Quotations
open FSharp.Quotations.Evaluator
open System.Reflection
open Autodesk.Revit.Mapper

type 'ut ExpressionContext = {
    lambdaExpr: Expr -> Expr
    bindings: Expr list
    output : Expr
    input: Expr
    defaultUT : Option<'ut>
    }

let finallize ctx = 
    let bindings = ctx.bindings |> List.reduce (fun p n -> Expr.Sequential(p,n))
    let lambda = Expr.Sequential(bindings,ctx.output) |> ctx.lambdaExpr
    let factory = lambda.CompileUntyped()
    factory

let castFactory fromType toType f =
    let factoryType = fsFuncType |> makeGenType [fromType;toType]
    Expr.Coerce(Val f,factoryType)

let handleIncludedType t simpleHandler entityHandler = 
    match t with
    |ValueType(t) -> t |> simpleHandler 
    |EntityType(def) -> def |> entityHandler

let arrayEntityHandler entities factory (fromType,toType) = 
    () 
     |> (Call mapInfo >> MakeGen [fromType; toType] >> With [factory;entities]) 
     |> (fun mapExpr -> Call toListInfo >> MakeGen [toType] >> With [mapExpr] <| ())
     |> Cast (genList toType)

let mapEntityHandler pairs factory (fromType,toType,key) = 
    let pair = makeGenType [key;fromType] kvPairType
    let input = Var("i",pair)
    let keySelector = Expr.NewDelegate(makeGenType [pair;key] csFuncType,[input],Expr.PropertyGet(Expr.Var input , keyInfo pair)) 
    let valueSelector = 
        Expr.NewDelegate(makeGenType [pair;toType] csFuncType,
                            [input], 
                            Expr.Application(factory,Expr.PropertyGet(Expr.Var input,valueInfo pair)))
    Call toDictionaryInfo >> MakeGen [pair;key;toType] >> With [pairs;keySelector;valueSelector] <| ()
     |> Cast (genDict [key;toType])

let exprBody fetch response typeResolver ctx = 
    let fetchList t = t |> genList |> fetch ctx
    let fetchDict t = t |> genDict |> fetch ctx
    let continueFromFactory cont def = 
        let (fromType,toType) = typeResolver def
        ctx.visitor def |> continueSuccess (fun f -> castFactory fromType toType <| f |> cont fromType toType )
    match ctx.eType with
    |Simple(t) -> t |> fetch ctx |> response ctx

    |Entity(def) -> def |> continueFromFactory
                     (fun from t f-> Expr.Application(f,fetch ctx from) |> response ctx)

    |Array(t) -> handleIncludedType t (fun tp-> tp |> fetchList |> response ctx)
                  (fun def -> def |> continueFromFactory 
                               (fun from t f -> (from |> fetchList, f, (from,t)) 
                                                 |||> arrayEntityHandler |> response ctx))

    |Map(key,value) -> handleIncludedType value (fun t -> [key;t] |> fetchDict |> response ctx)
                        (fun def -> def |> continueFromFactory 
                                     (fun from t f-> ([key;from] |> fetchDict, f ,(from,t,key))
                                                      |||> mapEntityHandler |> response ctx))

let readUnit (info:MemberInfo) =
    match info.GetCustomAttribute<UnitAttribute>() with
    |null -> None
    |attr -> Some(attr.DisplayType)

let fetchUnitType callback ctx = 
    match List.contains ctx.info.PropertyType havingUnitType with
    |false -> callback None
    |true -> match ctx.info |> readUnit with
             |Some(_) as ut -> callback ut
             |None -> match ctx.stepState.defaultUT with
                             | Some(_) as ut -> callback ut
                             | None -> failwith ""

let createNewCtx exprCtx expr = {exprCtx with bindings = expr :: exprCtx.bindings}  |> Success
