namespace Rebar.Common
{
    internal static class CommonNodes
    {
        static CommonNodes()
        {
            var builder = new NodeSignatureBuilder("ImmutablePassthrough");
            builder.DefinePassthroughParameter(false, builder.DefineGenericTypeParameter("T"));
            ImmutablePassthrough = builder.CreateNodeSignature();

            builder = new NodeSignatureBuilder("MutablePassthrough");
            builder.DefinePassthroughParameter(true, builder.DefineGenericTypeParameter("T"));
            MutablePassthrough = builder.CreateNodeSignature();

            builder = new NodeSignatureBuilder("Drop");
        }

        public static NodeSignature ImmutablePassthrough { get; }

        public static NodeSignature MutablePassthrough { get; }

        public static NodeSignature Drop { get; }
    }
}
