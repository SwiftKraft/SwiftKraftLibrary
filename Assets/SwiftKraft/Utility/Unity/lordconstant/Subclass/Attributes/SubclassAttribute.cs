// Credit: https://github.com/lordconstant/SubclassPropertyDrawer

using UnityEngine;

public class SubclassAttribute : PropertyAttribute
{
    public bool IncludeSelf = false;
    public bool IsList = false;

    public SubclassAttribute(bool InIncludeSelf = false, bool InIsList = false)
    {
        IncludeSelf = InIncludeSelf;
        IsList = InIsList;
    }
}
