namespace Rebar.Common
{
    internal interface ITypeUnificationResult
    {
        void SetTypeMismatch();

        void SetExpectedMutable();

        void AddFailedTypeConstraint(Constraint constraint);
    }
}
