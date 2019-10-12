namespace ReverseDNSGeolocation.Features
{
    using System;

    public class CompoundName
    {
        public string FullName { get; set; }

        public string FirstComponent { get; set; }

        public string SecondComponent { get; set; }

        public string ThirdComponent { get; set; }

        public CompoundName(string fullName, string firstComponent, string secondComponent)
        {
            this.FullName = fullName;
            this.FirstComponent = firstComponent;
            this.SecondComponent = secondComponent;
        }

        public CompoundName(string fullName, string firstComponent, string secondComponent, string thirdComponent)
        {
            this.FullName = FullName;
            this.FirstComponent = firstComponent;
            this.SecondComponent = secondComponent;
            this.ThirdComponent = thirdComponent;
        }

        public override int GetHashCode()
        {
            return this.FullName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var otherCompound = obj as CompoundName;
            return this.FullName.Equals(otherCompound?.FullName);
        }
    }
}
