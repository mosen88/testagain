#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Demo3D.Utilities;
using Demo3D.Visuals;
#endregion

namespace PalletConveyorCatalog.PalletConveyorToolBox
{
    internal class LoadDimensions
    {
        /// <summary>
        /// Tries to return the given height for the given visual.
        /// </summary>
        /// <param name="visual">The visual to set the color to.</param>
        /// <param name="color">The color to paint the visual with.</param>
        /// <param name="errorOnUnavailable">If <code>true</code> an exception is thrown on unsuccessful painting.</param>
        internal static DistanceProperty GetHeight(Visual visual, bool errorOnUnavailable = false)
        {
            // Easy on box visuals
            if (visual is BoxVisual)
            {
                DistanceProperty height = ((BoxVisual)visual).Height;
                return height;
            }
            if (visual is ContainerVisual)
            {
                DistanceProperty height = ((ContainerVisual)visual).Height;
                return height;
            }
            // Change only main color on imported mesh visuals
            if (visual is ImportedMeshVisual)
            {
                DistanceProperty height = ((ImportedMeshVisual)visual).Height;
                return height;
            }
            // Give up OR raise exception, if desired
            if (errorOnUnavailable) throw new InvalidOperationException("Cannot get height of the given visual: " + visual);
            else return null;
        }

        /// <summary>
        /// Tries to return the given depth/length for the given visual.
        /// </summary>
        /// <param name="visual"></param>
        /// <param name="errorOnUnavailable"></param>
        /// <returns></returns>
        internal static DistanceProperty GetDepth(Visual visual, bool errorOnUnavailable = false)
        {
            // Easy on box visuals
            if (visual is BoxVisual)
            {
                DistanceProperty depth = ((BoxVisual)visual).Depth;
                return depth;
            }
            if (visual is ContainerVisual)
            {
                DistanceProperty depth = ((ContainerVisual)visual).Depth;
                return depth;
            }
            // Change only main color on imported mesh visuals
            if (visual is ImportedMeshVisual)
            {
                DistanceProperty depth = ((ImportedMeshVisual)visual).Depth;
                return depth;
            }
            // Give up OR raise exception, if desired
            if (errorOnUnavailable) throw new InvalidOperationException("Cannot get depth of the given visual: " + visual);
            else return null;

        }


        /// <summary>
        /// Tries to return the given width for the given visual.
        /// </summary>
        /// <param name="visual"></param>
        /// <param name="errorOnUnavailable"></param>
        /// <returns></returns>
        internal static DistanceProperty GetWidth(Visual visual, bool errorOnUnavailable = false)
        {
            // Easy on box visuals
            if (visual is BoxVisual)
            {
                DistanceProperty width = ((BoxVisual)visual).Width;
                return width;
            }
            if (visual is ContainerVisual)
            {
                DistanceProperty width = ((ContainerVisual)visual).Width;
                return width;
            }
            // Change only main color on imported mesh visuals
            if (visual is ImportedMeshVisual)
            {
                DistanceProperty width = ((ImportedMeshVisual)visual).Width;
                return width;
            }
            // Give up OR raise exception, if desired
            if (errorOnUnavailable) throw new InvalidOperationException("Cannot get width of the given visual: " + visual);
            else return null;

        }
    }
}
