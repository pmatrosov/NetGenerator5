﻿using NetGenerator5.Generator.Dependency;

namespace NetGenerator5.Model
{
    [Model]
    public record ExampleModel
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    [Model]
    public class ExampleModel2
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
