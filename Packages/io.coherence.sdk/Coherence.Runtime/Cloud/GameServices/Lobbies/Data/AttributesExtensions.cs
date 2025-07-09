// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System.Collections.Generic;

    internal static class AttributesExtensions
    {

        public static void MergeAttributesWith(this LobbyPlayer player, List<CloudAttribute> newAttributes)
        {
            MergeAttributesWith(ref player.attributes, newAttributes);
        }

        public static void MergeAttributesWith(this LobbyData lobby, List<CloudAttribute> newAttributes)
        {
            MergeAttributesWith(ref lobby.lobbyAttributes, newAttributes);
        }

        private static void MergeAttributesWith(ref List<CloudAttribute> oldAttributes, List<CloudAttribute> newAttributes)
        {
            if (oldAttributes != null)
            {
                for (int i = 0; i < oldAttributes.Count; i++)
                {
                    var attribute = oldAttributes[i];

                    if (newAttributes.Count == 0)
                    {
                        break;
                    }

                    for (int j = newAttributes.Count - 1; j >= 0; j--)
                    {
                        var newAttribute = newAttributes[j];

                        if (!newAttribute.Key.Equals(attribute.Key))
                        {
                            continue;
                        }

                        newAttributes.RemoveAt(j);
                        oldAttributes[i] = newAttribute;
                    }
                }

                if (newAttributes.Count > 0)
                {
                    oldAttributes.AddRange(newAttributes);
                }
            }
            else
            {
                oldAttributes = newAttributes;
            }
        }
    }
}
