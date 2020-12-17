namespace RevitApiEntityMapper.tests
open NUnit.Framework
open ReflectedClasses
open Helpers
open System
open Autodesk.Revit.DB.ExtensibleStorage
open System.Collections.Generic
open System.Linq

[<TestFixture>]
type SetterTests ()=
    
    let setEntity e = execInTransaction (fun () -> getWall().SetEntity e |> ignore)
    let getEntity schema = getWall().GetEntity(schema)

    [<SetUp>]
    member this.SetUp() = setUp()

    [<Test>]
    member this.ShouldSetValue () = 
        testSetterWith<Bool> 
            (fun schema factory -> let obj = Bool()
                                   obj.Some <- true
                                   let e = factory obj
                                   setEntity e
                                   getEntity(schema).Get<bool> "Some" |> assertThat true)
    
    [<Test>]
    member this.ShouldSetWithIncludedEntity () =
        testSetterWith<IncludedEntity> 
           (fun schema factory -> let obj = IncludedEntity ()
                                  let included = Bool()
                                  included.Some <- true
                                  obj.Some <- included
                                  let e = factory obj
                                  setEntity e
                                  getEntity(schema)
                                    .Get<Entity>("Some")
                                    .Get<bool> "Some" |> assertThat true)

    [<Test>]
    member this.ShouldSetInvalidEntityThenNull () = 
        testSetterWith<IncludedEntity>
            (fun schema factory -> let obj = IncludedEntity()
                                   let e = factory obj
                                   setEntity e
                                   getEntity(schema)
                                        .Get<Entity>("Some").IsValid() |> assertThat false)

    [<Test>]
    member this.ShouldSetReqursiveEntity () =
        testSetterWith<Recursive>
            (fun schema factory -> let obj = Recursive()
                                   obj.Some <- Recursive()
                                   let e = factory obj
                                   setEntity e
                                   let entity = getEntity(schema).Get<Entity>("Some")
                                   entity.IsValid() |> assertThat true
                                   entity.Get<Entity>("Some").IsValid() |> assertThat false)
                                   
    
    [<Test>]
    member this.ShouldSetListValues () =
        testSetterWith<List> 
           (fun schema factory -> let obj = List()
                                  let included = List<bool>(seq{true})
                                  obj.Some <- included
                                  let e = factory obj
                                  setEntity e
                                  getEntity(schema)
                                    .Get<IList<bool>>("Some").First() |> assertThat true)

    [<Test>]
    member this.Shouldn'tSetNullProperties () =
        testSetterWith<ElementTree>
            (fun schema factory -> let obj = ElementTree()
                                   let e = factory obj
                                   setEntity e
                                   getEntity(schema).IsValid() |> assertThat true)

    [<Test>]
    member this.ShouldSetListWithIncludedEntities () =
        testSetterWith<IncludedEntityList> 
              (fun schema factory -> let obj = IncludedEntityList()
                                     let included = List<Bool>(seq{Bool()})
                                     obj.Some <- included
                                     let e = factory obj
                                     setEntity e
                                     getEntity(schema)
                                        .Get<IList<Entity>>("Some").First()
                                        .Get<bool>("Some") |> assertThat false)

    [<Test>]
    member this.ShouldSetDictWithValues () =
        testSetterWith<Dictionary> 
           (fun schema factory -> let obj = Dictionary()
                                  let included = Dictionary<string,bool>()
                                  included.Add("key",true)
                                  obj.Some <- included
                                  let e = factory obj
                                  setEntity e
                                  let first = getEntity(schema).Get<IDictionary<string,bool>>("Some").First()
                                  first.Value |> assertThat true
                                  first.Key |> assertThat "key")

    [<Test>]
    member this.ShouldSetDictWithIncludedEntities () =
        testSetterWith<IncludedEntityInDictionary> 
           (fun schema factory -> let obj = IncludedEntityInDictionary()
                                  let included = Dictionary<string,Bool>()
                                  let bl = Bool()
                                  bl.Some <- true
                                  included.Add("key",bl)
                                  obj.Some <- included
                                  let e = factory obj
                                  setEntity e
                                  let first = getEntity(schema).Get<IDictionary<string,Entity>>("Some").First()
                                  first.Value.Get<bool>("Some") |> assertThat true
                                  first.Key |> assertThat "key")
