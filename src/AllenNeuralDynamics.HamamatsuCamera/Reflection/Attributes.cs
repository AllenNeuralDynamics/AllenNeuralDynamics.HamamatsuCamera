using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllenNeuralDynamics.HamamatsuCamera.Reflection
{
    public class CustomSortedCategoryAttribute : CategoryAttribute
    {
        private const char NonPrintableChar = '\t';

        public CustomSortedCategoryAttribute(string category, ushort categoryPos, ushort totalCategories)
            : base(category.PadLeft(category.Length + (totalCategories - categoryPos), CustomSortedCategoryAttribute.NonPrintableChar))
        {
        }
    }
}
