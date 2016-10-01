#if !EXCLUDE_CODEGEN
#pragma warning disable 162
#pragma warning disable 219
#pragma warning disable 414
#pragma warning disable 649
#pragma warning disable 693
#pragma warning disable 1591
#pragma warning disable 1998
[assembly: global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.3.0")]
[assembly: global::Orleans.CodeGeneration.OrleansCodeGenerationTargetAttribute("Orleans.StorageProviders.RedisStorage.GrainInterfaces, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
namespace Orleans.StorageProviders.RedisStorage.GrainInterfaces
{
    using global::Orleans.Async;
    using global::Orleans;

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.3.0"), global::System.SerializableAttribute, global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute, global::Orleans.CodeGeneration.GrainReferenceAttribute(typeof (global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1))]
    internal class OrleansCodeGenGrain1Reference : global::Orleans.Runtime.GrainReference, global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1
    {
        protected @OrleansCodeGenGrain1Reference(global::Orleans.Runtime.GrainReference @other): base (@other)
        {
        }

        protected @OrleansCodeGenGrain1Reference(global::System.Runtime.Serialization.SerializationInfo @info, global::System.Runtime.Serialization.StreamingContext @context): base (@info, @context)
        {
        }

        protected override global::System.Int32 InterfaceId
        {
            get
            {
                return 1743709865;
            }
        }

        public override global::System.String InterfaceName
        {
            get
            {
                return "global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1";
            }
        }

        public override global::System.Boolean @IsCompatible(global::System.Int32 @interfaceId)
        {
            return @interfaceId == 1743709865;
        }

        protected override global::System.String @GetMethodName(global::System.Int32 @interfaceId, global::System.Int32 @methodId)
        {
            switch (@interfaceId)
            {
                case 1743709865:
                    switch (@methodId)
                    {
                        case -394250501:
                            return "Set";
                        case -940922787:
                            return "Get";
                        default:
                            throw new global::System.NotImplementedException("interfaceId=" + 1743709865 + ",methodId=" + @methodId);
                    }

                default:
                    throw new global::System.NotImplementedException("interfaceId=" + @interfaceId);
            }
        }

        public global::System.Threading.Tasks.Task @Set(global::System.String @stringValue, global::System.Int32 @intValue, global::System.DateTime @dateTimeValue, global::System.Guid @guidValue, global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1 @grainValue)
        {
            return base.@InvokeMethodAsync<global::System.Object>(-394250501, new global::System.Object[]{@stringValue, @intValue, @dateTimeValue, @guidValue, @grainValue is global::Orleans.Grain ? @grainValue.@AsReference<global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1>() : @grainValue});
        }

        public global::System.Threading.Tasks.Task<global::System.Tuple<global::System.String, global::System.Int32, global::System.DateTime, global::System.Guid, global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1>> @Get()
        {
            return base.@InvokeMethodAsync<global::System.Tuple<global::System.String, global::System.Int32, global::System.DateTime, global::System.Guid, global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1>>(-940922787, null);
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Orleans-CodeGenerator", "1.2.3.0"), global::Orleans.CodeGeneration.MethodInvokerAttribute("global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1", 1743709865, typeof (global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1)), global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    internal class OrleansCodeGenGrain1MethodInvoker : global::Orleans.CodeGeneration.IGrainMethodInvoker
    {
        public global::System.Threading.Tasks.Task<global::System.Object> @Invoke(global::Orleans.Runtime.IAddressable @grain, global::Orleans.CodeGeneration.InvokeMethodRequest @request)
        {
            global::System.Int32 interfaceId = @request.@InterfaceId;
            global::System.Int32 methodId = @request.@MethodId;
            global::System.Object[] arguments = @request.@Arguments;
            try
            {
                if (@grain == null)
                    throw new global::System.ArgumentNullException("grain");
                switch (interfaceId)
                {
                    case 1743709865:
                        switch (methodId)
                        {
                            case -394250501:
                                return ((global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1)@grain).@Set((global::System.String)arguments[0], (global::System.Int32)arguments[1], (global::System.DateTime)arguments[2], (global::System.Guid)arguments[3], (global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1)arguments[4]).@Box();
                            case -940922787:
                                return ((global::Orleans.StorageProviders.RedisStorage.GrainInterfaces.IGrain1)@grain).@Get().@Box();
                            default:
                                throw new global::System.NotImplementedException("interfaceId=" + 1743709865 + ",methodId=" + methodId);
                        }

                    default:
                        throw new global::System.NotImplementedException("interfaceId=" + interfaceId);
                }
            }
            catch (global::System.Exception exception)
            {
                return global::Orleans.Async.TaskUtility.@Faulted(exception);
            }
        }

        public global::System.Int32 InterfaceId
        {
            get
            {
                return 1743709865;
            }
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
