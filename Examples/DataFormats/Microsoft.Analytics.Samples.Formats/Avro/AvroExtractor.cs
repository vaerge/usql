﻿// 
// Copyright (c) Microsoft and contributors.  All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// 
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System.Collections.Generic;
using Microsoft.Analytics.Interfaces;
using Avro.File;
using Avro.Generic;
using System.IO;

namespace Microsoft.Analytics.Samples.Formats.ApacheAvro
{
    [SqlUserDefinedExtractor(AtomicFileProcessing = true)]
    public class AvroExtractor : IExtractor
    {
        private string avroSchema;

        public AvroExtractor(string avroSchema)
        {
            this.avroSchema = avroSchema;
        }

        public override IEnumerable<IRow> Extract(IUnstructuredReader input, IUpdatableRow output)
        {
            var avschema = Avro.Schema.Parse(avroSchema);
            var reader = new GenericDatumReader<GenericRecord>(avschema, avschema);

            using (var ms = new MemoryStream())
            {
                CreateSeekableStream(input, ms);
                ms.Position = 0;

                var fileReader = DataFileReader<GenericRecord>.OpenReader(ms, avschema);

                while (fileReader.HasNext())
                {
                    var avroRecord = fileReader.Next();

                    foreach (var column in output.Schema)
                    {
                        if (avroRecord[column.Name] != null)
                        {
                            output.Set(column.Name, avroRecord[column.Name]);
                        }
                        else
                        {
                            output.Set<object>(column.Name, null);
                        }

                        yield return output.AsReadOnly();
                    }
                }
            }
        }

        private void CreateSeekableStream(IUnstructuredReader input, MemoryStream output)
        {
            input.BaseStream.CopyTo(output);
        }
    }
}
