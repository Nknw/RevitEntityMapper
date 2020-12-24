# RevitEntityMapper

Revit API Entity mapper based on LINQ Expressions.

[![NuGet Link](https://img.shields.io/nuget/v/Revit.EntityMapper)](https://www.nuget.org/packages/Revit.EntityMapper/)
## Usage

```csharp
using Revit.EntityMapper;
```
### Define classes
```csharp

[Schema("3ef20639-1768-49c0-8cf3-ef4c6f717369", nameof(Price))]
public class Price
{
    public double Value { get; set; }
    public string Currency { get; set; }
}

[Schema("396c663b-3b48-47d7-bbea-e62ed0958c07",nameof(Producer))]
public class Producer
{
    public string Name { get; set; }
}

[Schema("e6e9bb2d-5041-4542-a73f-65b025db20ce",nameof(Product))]
public class Product
{
    public Price Price { get; set; }
    public Producer Producer { get; set; }
}
```
[Samples of using attributes](https://github.com/Nknw/RevitEntityMapper/blob/master/Samples/ReflectedClasses/Features.cs)

### Create mapper
Constructing a mapper is expensive in comparison to a invocation of the mapper. Therefore, it's better to use singleton or long-lived object for storing the mapper.
```csharp
public static class MapperInstance
{
    private readonly static IMapper<Product> productMapper 
        = Mapper.CreateAdHoc<Product>();

    private readonly static IMapper mapper
        = Mapper.CreateNew(); 

    public static IMapper<Product> GetProductMapper() 
        => productMapper;

    public static IMapper GetGenericMapper()
        => mapper;
}
```
AdHoc mapper expands schema in memory
```csharp
public Product GetProduct(Element element)
{
    return MapperInstance.GetProductMapper()
        .GetEntity(element);
}

public void ReplaceNullProducer(Element element, DataStorage producerStorage)
{
    var mapper = MapperInstance.GetGenericMapper();
    var producer = mapper.Get<Producer>(producerStorage);
    var product = mapper.Get<Product>(element);
    product.Producer ??= producer;
    mapper.Set(element,product);
}
```
## License

MIT