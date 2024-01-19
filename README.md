![Detached Banner](banner.png?raw=true)
# Runtime Types
#### What is it

This library tries to simplify the runtime type creation by adding new things like the ability to define methods using Expressions and 
automatic interface implementation.
It's a part of [Detached.Mappers](https://github.com/leonardoporro/Detached-Mapper) library.
Thanks to the people who made [FastExpressionCompiler](https://github.com/dadhi/FastExpressionCompiler), that is the core of this library.

## What does it solve
 It allows devs to create tools like dynamic proxies, comparers, dirty check (INotifyPropertyChange) or any other thing 
 that may be a good fit for dynamic code without having to manually emit op codes. Methods can be defined using Expression trees.

### How it works
Lets say that we want to dinamically create a type for this interface:
```csharp
 public interface ISumService
 {
     int Sum(int a, int b);
 }
```
Then we need to intialize a type builder, define an implementation for Sum method using an expression tree, and 
call AutoImplementInterface to perform the override.

```csharp
// create a type builder, this will handle the creation of a new Type.
RuntimeTypeBuilder typeBuilder = new RuntimeTypeBuilder("MyISumServiceImplementation", typeof(BasePropertyClass));

// define the parameters expected by the interface method.
var aParam = Parameter(typeof(int), "a");
var bParam = Parameter(typeof(int), "b");

// define a mehtod with the same signature (name and parameters),
// use "Add" expression as the method body
typeBuilder.DefineMethod(
    "Sum",
        new[] { aParam, bParam },
        Block(
        Add(aParam, bParam)
        )
);
            
// implement the interface, this will iterate over all methods and 
// call DefineMethodOverride to bind the existing methods to the given 
// interface methods
typeBuilder.AutoImplementInterface(typeof(ISumService));

// create the type that will be used to initialize new instances of ISumService
Type myISumServiceType = typeBuilder.Create();

// create an instance of our dynamic ISumService implementation
ISumService myISumService = (ISumService)Activator.CreateInstance(myISumServiceType);

// test the implementation
Assert.Equal(14, myISumService.Sum(5, 9));

```

More info and examples will be added later.
Check unit tests for more samples!
 
