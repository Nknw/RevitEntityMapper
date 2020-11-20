namespace RevitApiEntityMapper.tests
open NUnit.Framework
open Helpers
open ReflectedClasses
open Autodesk.Revit.DB

[<TestFixture>]
type FeatureTests () =
    
    [<SetUp>]
    member this.SetUp()= setUp ()

    [<Test>]
    member this.ShouldWriteDocumentation () = 
        typeof<Documentation> 
         |> testCreatorWith
          (fun s-> s.Documentation |> assertThat "doc"
                   s.GetField("Some").Documentation |> assertThat "prop doc")

    [<Test>]
    member this.ShouldWritePermissions () =
        typeof<Permissions>
         |> testCreatorWith
          (fun s -> s.VendorId |> assertThat "ADSK")

    [<Test>]
    member this.ShouldWriteUnits () = 
        typeof<Unit>
         |> testCreatorWith
          (fun s -> s.GetField("Length").UnitType |> assertThat UnitType.UT_Length)

    [<Test>]
    member this.ShouldOverrideUnits () =
        typeof<OverrideDefaultUnits> 
         |> testCreatorWith
          (fun s -> s.GetField("Width").UnitType |> assertThat UnitType.UT_Mass
                    s.GetField("Length").UnitType |> assertThat UnitType.UT_Length)

    [<Test>]
    member this.ShouldExcludeProperties () =
        typeof<ExcludeProperty>
         |> testCreatorWith
          (fun s -> s.ListFields().Count |> assertThat 1)
    

