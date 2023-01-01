using Detached.RuntimeTypes.Reflection;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;
using static Detached.RuntimeTypes.Expressions.ExtendedExpression;
using static System.Linq.Expressions.Expression;

namespace Detached.RuntimeTypes.Tests
{
    public class RuntimeTypeTests
    {
        [Fact]
        public void define_property()
        {
            RuntimeTypeBuilder typeBuilder = new RuntimeTypeBuilder("DefineAutoProperty");

            var fieldInfo = typeBuilder.DefineField("_testProp", typeof(int), FieldAttributes.Private);
            var field = Expression.Field(typeBuilder.This, fieldInfo);

            var value = Expression.Parameter(typeof(int), "value");

            typeBuilder.DefineProperty("TestProp", 
                typeof(int),
                field,
                value,
                Assign(field, value));

            Type newType = typeBuilder.Create();
            object newInstance = Activator.CreateInstance(newType);

            PropertyInfo testPropInfo = newType.GetProperty("TestProp");
            testPropInfo.SetValue(newInstance, 5);
            int result = (int)testPropInfo.GetValue(newInstance);

            Assert.Equal(5, result);
        }

        [Fact]
        public void define_autoproperty()
        {
            RuntimeTypeBuilder typeBuilder = new RuntimeTypeBuilder("DefineProperty");
            typeBuilder.DefineAutoProperty("TestProp", typeof(int));

            Type newType = typeBuilder.Create();
            object newInstance = Activator.CreateInstance(newType);

            PropertyInfo testPropInfo = newType.GetProperty("TestProp");
            testPropInfo.SetValue(newInstance, 5);
            int result = (int)testPropInfo.GetValue(newInstance);

            Assert.Equal(5, result);
        }

        [Fact]
        public void define_method()
        {
            RuntimeTypeBuilder typeBuilder = new RuntimeTypeBuilder("DefineMethod");
            ParameterExpression a = Parameter(typeof(int), "a");
            ParameterExpression b = Parameter(typeof(int), "b");

            typeBuilder.DefineMethod("TestMethod",
                new[] { a, b },
                Add(a, b)
            );

            Type newType = typeBuilder.Create();
            object newInstance = Activator.CreateInstance(newType);

            MethodInfo testMethodInfo = newType.GetMethod("TestMethod");
            object result = testMethodInfo.Invoke(newInstance, new object[] { 5, 4 });

            Assert.Equal(9, result);
        }

        [Fact]
        public void access_local_field()
        {
            RuntimeTypeBuilder typeBuilder = new RuntimeTypeBuilder("AccessLocalField");

            var fieldInfo = typeBuilder.DefineField("TestField", typeof(string));
            var field = Expression.Field(typeBuilder.This, fieldInfo);

            typeBuilder.DefineMethod("TestMethod", null, field);

            Type newType = typeBuilder.Create();
            object newInstance = Activator.CreateInstance(newType);

            var fieldToSet = newType.GetField("TestField");
            fieldToSet.SetValue(newInstance, "testValue");
            MethodInfo testMethodInfo = newType.GetMethod("TestMethod");
            object result = testMethodInfo.Invoke(newInstance, null);

            Assert.Equal("testValue", result);
        }

        [Fact]
        public void call_base_method()
        {
            RuntimeTypeBuilder typeBuilder = new RuntimeTypeBuilder("CallBaseMethod", typeof(BaseMethodClass));
            MethodInfo getTextInfo = typeof(BaseMethodClass).GetMethod("GetText");

            typeBuilder.OverrideMethod(getTextInfo,
                null,
                Call("Concat", typeof(string), Call(typeBuilder.Base, getTextInfo), Constant(" (overriden)"))
            );

            Type newType = typeBuilder.Create();
            object newInstance = Activator.CreateInstance(newType);

            MethodInfo testMethodInfo = newType.GetMethod("GetText");
            object result = testMethodInfo.Invoke(newInstance, null);

            Assert.Equal("this is the base class! (overriden)", result);
        }

        [Fact]
        public void override_property()
        {
            RuntimeTypeBuilder typeBuilder = new RuntimeTypeBuilder("OverrideProperty", typeof(BasePropertyClass));

            PropertyInfo propInfo = typeof(BasePropertyClass).GetProperty("Text");

            typeBuilder.OverrideMethod(propInfo.GetGetMethod(),
                null,
                Call("Concat", typeof(string), Call(typeBuilder.Base, propInfo.GetGetMethod()), Constant(" (overriden)"))
            );

            Type newType = typeBuilder.Create();
            object newInstance = Activator.CreateInstance(newType);

            propInfo.SetValue(newInstance, "this is a property!");
            string result = (string)propInfo.GetValue(newInstance);

            Assert.Equal("this is a property! (overriden)", result);
        }

        [Fact]
        public void auto_implement_interface()
        {
            RuntimeTypeBuilder typeBuilder = new RuntimeTypeBuilder("AutoImplenetInterface", typeof(BasePropertyClass));
            typeBuilder.AutoImplementInterface(typeof(ITextProperty));

            Type newType = typeBuilder.Create();
            ITextProperty newInstance = (ITextProperty)Activator.CreateInstance(newType);

            newInstance.Text = "test text";

            Assert.Equal("test text", newInstance.Text);
        }

        [Fact]
        public void build_implementation()
        {
            // create a type builder, this will handle the creation of 
            // a new Type.
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
        }

        public interface ISumService
        {
            int Sum(int a, int b);
        }

        public class BasePropertyClass
        {
            public virtual string Text { get; set; }
        }

        public interface ITextProperty
        {
            string Text { get; set; }
        }

        public class BaseMethodClass
        {
            public virtual string GetText() => "this is the base class!";
        }
    }
}