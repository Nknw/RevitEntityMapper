module ExprHelpers
open System.Reflection
open FSharp.Quotations
open System
open System.Collections.Generic
open Abstractions
open FSharp.Quotations.Evaluator
open System.Linq
open Autodesk.Revit.DB
open Autodesk.Revit.Mapper

let getMethodInfo (e : Expr<'T>) : MethodInfo =
  match e with
  | Patterns.Call (_, mi, _) -> mi
  | _ -> failwithf "Expression has the wrong shape, expected Call (_, _, _) instead got: %A" e

let genericMethodInfo (e : Expr<'T>) : MethodInfo = let typedInfo = getMethodInfo e
                                                    typedInfo.GetGenericMethodDefinition ()

let makeGenType types (m:Type) = m.MakeGenericType (types|>List.toArray)

let havingUnitType = [typeof<float>;typeof<XYZ>;typeof<double>;typeof<UV>] 

let fsFuncType = typedefof<int->int>
let csFuncType = typedefof<Func<int,int>>
let kvPairType = typedefof<KeyValuePair<obj,obj>>
let lst = List<string> ()
let mapInfo = <@Seq.map (fun c->c) (seq{"1"}) @> |> genericMethodInfo
let toListInfo = <@ lst.ToList ()@> |> genericMethodInfo
let toDictionaryInfo = <@lst.ToDictionary
                          (Func<string,string>(fun s->s),Func<string,string>(fun s->s))@> |> genericMethodInfo

let keyInfo (t:Type) = t.GetProperty("Key")

let valueInfo (t:Type) = t.GetProperty("Value")

type 'info ExprContext = {
    obj: Option<Expr>
    memberInfo: 'info
    }

let Call (mi:MethodInfo) () =
    {memberInfo = mi;obj = Option.None}

let On obj ctx =
    {ctx with obj = Some(obj)}

let With args (ctx:ExprContext<MethodInfo>) = 
    match ctx.obj with
        |Some(o) -> Expr.Call(o,ctx.memberInfo,args)
        |Option.None -> Expr.Call(ctx.memberInfo,args)

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
    {memberInfo= pi;obj=Option.None}


let To value (ctx:ExprContext<PropertyInfo>) = 
    match ctx.obj with
        |Some(o) -> Expr.PropertySet(o,ctx.memberInfo,value)
        |Option.None -> Expr.PropertySet(ctx.memberInfo,value)

let Val constant = Expr.Value constant


type 'ut ExpressionContext = {
    lambdaExpr: Expr -> Expr
    bindings: Expr list
    output : Expr
    input: Expr
    defaultUT : Option<'ut>
    }

let finallize (factories:Dictionary<Type,obj>) ctx = 
    let bindings = ctx.bindings |> List.reduce (fun p n -> Expr.Sequential(p,n))
    let lambda = Expr.Sequential(bindings,ctx.output) |> ctx.lambdaExpr
    let factory = lambda.CompileUntyped()
    factories.Add(ctx.output.Type,factory)
    factory |> Success

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

let mapEntityHandler pairs factory (fromType,toType,key) = 
    let pair = makeGenType [key;fromType] kvPairType
    let input = Var("i",pair)
    let keySelector = Expr.NewDelegate(makeGenType [pair;key] csFuncType,[input],Expr.PropertyGet(Expr.Var input , keyInfo pair)) 
    let valueSelector = 
        Expr.NewDelegate(makeGenType [pair;toType] csFuncType,
                            [input], 
                            Expr.Application(factory,Expr.PropertyGet(Expr.Var input,valueInfo pair)))
    Call toDictionaryInfo >> MakeGen [pair;key;toType] >> With [pairs;keySelector;valueSelector] <| ()

let exprBody fetch response typeResolver ctx = 
    let fetchList t = t |> genList |> fetch ctx
    let fetchDict t = t |> genDict |> fetch ctx
    let continueFromFactory cont def = 
        let (fromType,toType) = typeResolver def
        ctx.visitor def |> continueSuccess (fun f -> castFactory fromType toType <| f |> cont fromType toType )
    match ctx.eType with
    |Simple(t) -> t |> fetch ctx |> response ctx

    |Entity(def) -> def |> continueFromFactory
                     (fun _ t f-> Expr.Application(f,fetch ctx t) |> response ctx)

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
    |null -> Option.None
    |attr -> Some(attr.DisplayType)

let fetchUnitType callback ctx = 
    match List.contains ctx.info.PropertyType havingUnitType with
    |false -> callback Option.None
    |true -> match ctx.info |> readUnit with
             |Some(_) as ut -> callback ut
             |Option.None -> match ctx.stepState.defaultUT with
                             | Some(_) as ut -> callback ut
                             | Option.None -> failwith ""

let createNewCtx exprCtx expr = {exprCtx with bindings = expr :: exprCtx.bindings}  |> Success
