// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Data source for the <see cref="AsynchronousTaggerProvider{TTag}"/>.  This type tells the
    /// <see cref="AsynchronousTaggerProvider{TTag}"/> when tags need to be recomputed, as well
    /// as producing the tags when requested.
    /// </summary>
    internal interface IAsynchronousTaggerDataSource<TTag> where TTag : ITag
    {
        /// <summary>
        /// Whether or not the <see cref="AsynchronousTaggerProvider{TTag}"/> should remove a tag
        /// from the user interface if the user makes an edit that intersects with the span of the
        /// tag.  Removing may be appropriate if it is undesirable for stale tag data to be 
        /// presented to the user.  However, removal may also lead to a more noticible tagging 
        /// experience for the user if tags quickly get removed and re-added.
        /// </summary>
        bool RemoveTagsThatIntersectEdits { get; }

        /// <summary>
        /// Creates the <see cref="ITaggerEventSource"/> that notifies the <see cref="AsynchronousTaggerProvider{TTag}"/>
        /// that it should recompute tags for the text buffer after an appropriate <see cref="TaggerDelay"/>.
        /// </summary>
        ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer);

        /// <summary>
        /// Creates the <see cref="ITagProducer{TTag}"/> which will be used by the 
        /// <see cref="AsynchronousTaggerProvider{TTag}"/> to produce tags asynchronously.
        /// </summary>
        ITagProducer<TTag> CreateTagProducer();
    }
}
