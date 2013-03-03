﻿﻿#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;
using System.Collections.Generic;
using System.IO;

namespace ModernMinas.Update.Api // MODIFICATION ModernMinas: changed to update API namespace
{
    /// <summary>
    /// Provides a stream wrapper that will not close/dispose the underlying stream
    /// </summary>
    class NonClosingStream : AggregateStream // MODIFICATION ModernMinas: Make it a private helper class
    {
        /// <summary> Creates a wrapper around the provided stream </summary>
        public NonClosingStream(Stream stream) 
            : base(stream)
        { }

        /// <summary> Disposes of this.Stream </summary>
        public override void Close()
        {
            base.Stream = null;
            base.Close();
        }

        /// <summary> Prevents the disposal of the aggregated stream </summary>
        protected override void Dispose(bool disposing)
        {
            base.Stream = null;
            base.Dispose(disposing);
        }
    }
}