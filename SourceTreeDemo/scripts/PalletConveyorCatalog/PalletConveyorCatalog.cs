#region Namespaces
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using Demo3D.Common;
using Demo3D.Gui;
using Demo3D.Native;
using Demo3D.Utilities;
using Demo3D.Visuals;
using Microsoft.DirectX;
#endregion

namespace PalletConveyorCatalog {
    [Auto] public class PalletConveyorCatalog {
        #region Examples
        [Auto] IBuilder app;
        [Auto] Document document;
        [Auto] PrintDelegate print;
        [Auto] VectorDelegate vector;

#if false
        /// <summary>
        /// Custom properties are automatically created and connected.
        /// Uncomment these by removing the leading '//' to see the effect.
        /// </summary>
        [DefaultValue("Hello World"),Description("Message description"),Category("Special")]
        [Auto] SimplePropertyValue<string> SomeMessage;
        [Auto] CustomPropertyValue<DistanceProperty> SomeDistance;

        /// <summary>
        /// Events that need to block return IEnumerable and yield return Wait.XXX(...) to pause.
        /// </summary>
        [Auto] IEnumerable OnInitialize(Visual sender) {
            print("OnInitialize: " + sender.Name);

            var document = sender.Document;
            while (true) {
                yield return Wait.ForSeconds(1);
                print("time = " + document.Time);
            }
        }

        /// <summary>
        /// Class can have no constructor, a constructor with no arguments or a 
        /// constructor with a single argument of type Visual.
        /// </summary>
        public PalletConveyorCatalog(Visual sender) {
            // This will force the binding of all [Auto] members now instead of after the constructor completes
            sender.SetNativeObject(this);
        }
#endif
        #endregion
 
        [Auto] void OnReset(Visual sender) {         
        }
    }
}