#if !EXCLUDE_CODEGEN
#pragma warning disable 162
#pragma warning disable 219
#pragma warning disable 414
#pragma warning disable 649
#pragma warning disable 693
#pragma warning disable 1591
#pragma warning disable 1998
[assembly: global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.3.0")]
[assembly: global::Orleans.CodeGeneration.OrleansCodeGenerationTargetAttribute("Orleans.StorageProviders.RedisStorage.GrainClasses, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
namespace Orleans.StorageProviders.RedisStorage.GrainClasses
{
    using global::Orleans.Async;
    using global::Orleans;

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.3.0"), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.SerializerAttribute(typeof (global::Orleans.StorageProviders.RedisStorage.GrainClasses.MyState)), global::Orleans.CodeGeneration.RegisterSerializerAttribute]
    internal class OrleansCodeGenOrleans_StorageProviders_RedisStorage_GrainClasses_MyStateSerializer
    {
        [global::Orleans.CodeGeneration.CopierMethodAttribute]
        public static global::System.Object DeepCopier(global::System.Object original)
        {
            global::Orleans.StorageProviders.RedisStorage.GrainClasses.MyState input = ((global::Orleans.StorageProviders.RedisStorage.GrainClasses.MyState)original);
            global::Orleans.StorageProviders.RedisStorage.GrainClasses.MyState result = new global::Orleans.StorageProviders.RedisStorage.GrainClasses.MyState();
            result.@DateTimeValue = input.@DateTimeValue;
            result.@Etag = input.@Etag;
            result.@GrainValue = (global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(input.@GrainValue);
            result.@GuidValue = (global::System.Guid)global::Orleans.Serialization.SerializationManager.@DeepCopyInner(input.@GuidValue);
            result.@IntValue = input.@IntValue;
            result.@StringValue = input.@StringValue;
            global::Orleans.@Serialization.@SerializationContext.@Current.@RecordObject(original, result);
            return result;
        }

        [global::Orleans.CodeGeneration.SerializerMethodAttribute]
        public static void Serializer(global::System.Object untypedInput, global::Orleans.Serialization.BinaryTokenStreamWriter stream, global::System.Type expected)
        {
            global::Orleans.StorageProviders.RedisStorage.GrainClasses.MyState input = (global::Orleans.StorageProviders.RedisStorage.GrainClasses.MyState)untypedInput;
            global::Orleans.Serialization.SerializationManager.@SerializeInner(input.@DateTimeValue, stream, typeof (global::System.DateTime));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(input.@Etag, stream, typeof (global::System.String));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(input.@GrainValue, stream, typeof (global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(input.@GuidValue, stream, typeof (global::System.Guid));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(input.@IntValue, stream, typeof (global::System.Int32));
            global::Orleans.Serialization.SerializationManager.@SerializeInner(input.@StringValue, stream, typeof (global::System.String));
        }

        [global::Orleans.CodeGeneration.DeserializerMethodAttribute]
        public static global::System.Object Deserializer(global::System.Type expected, global::Orleans.Serialization.BinaryTokenStreamReader stream)
        {
            global::Orleans.StorageProviders.RedisStorage.GrainClasses.MyState result = new global::Orleans.StorageProviders.RedisStorage.GrainClasses.MyState();
            global::Orleans.@Serialization.@DeserializationContext.@Current.@RecordObject(result);
            result.@DateTimeValue = (global::System.DateTime)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.DateTime), stream);
            result.@Etag = (global::System.String)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.String), stream);
            result.@GrainValue = (global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1), stream);
            result.@GuidValue = (global::System.Guid)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Guid), stream);
            result.@IntValue = (global::System.Int32)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.Int32), stream);
            result.@StringValue = (global::System.String)global::Orleans.Serialization.SerializationManager.@DeserializeInner(typeof (global::System.String), stream);
            return (global::Orleans.StorageProviders.RedisStorage.GrainClasses.MyState)result;
        }

        public static void Register()
        {
            global::Orleans.Serialization.SerializationManager.@Register(typeof (global::Orleans.StorageProviders.RedisStorage.GrainClasses.MyState), DeepCopier, Serializer, Deserializer);
        }

        static OrleansCodeGenOrleans_StorageProviders_RedisStorage_GrainClasses_MyStateSerializer()
        {
            Register();
        }
    }
}
#pragma warning restore 162
#pragma warning restore 219
#pragma warning restore 414
#pragma warning restore 649
#pragma warning restore 693
#pragma warning restore 1591
#pragma warning restore 1998
#endif
