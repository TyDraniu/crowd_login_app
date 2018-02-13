using System;
using System.Collections.Generic;

namespace StashRest
{
    public class Link
    {
        public Uri url { get; set; }
        public string rel { get; set; }
    }

    public class Links
    {
        public Self self { get; set; }
    }

    public class Self
    {
        public List<Uri> href { get; private set; }
    }

    public class Author
    {
        public string name { get; set; }
        public string emailAddress { get; set; }
    }

    public class Errors
    {
        public string context { get; set; }
        public string message { get; set; }
        public string exceptionName { get; set; }
    }

    public class PValue
    {
        public string key { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public bool @public { get; set; }
        public string type { get; set; }
        public Link link { get; set; }
        public Links links { get; set; }
        public string slug { get; set; }
        public string scmId { get; set; }
        public string state { get; set; }
        public string statusMessage { get; set; }
        public bool forkable { get; set; }
        public string displayId { get; set; }
        public Author author { get; set; }
        public double authorTimestamp { get; set; }
        public string message { get; set; }
    }

    public class Project
    {
        public int size { get; set; }
        public int limit { get; set; }
        public bool isLastPage { get; set; }
        public List<PValue> values { get; private set; }
        public int start { get; set; }
        public List<Errors> errors { get; private set; }
    }
}
