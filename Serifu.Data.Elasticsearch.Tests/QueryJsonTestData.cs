// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

namespace Serifu.Data.Elasticsearch.Tests;

internal sealed class QueryJsonTestData : TheoryData<string, string, string> // query, json, because
{
    public QueryJsonTestData()
    {
        Add(because: "a query in Japanese should search the Japanese translation",
            query: "能代",
            json: /*lang=json,strict*/ """
            {
              "bool": {
                "minimum_should_match": 1,
                "should": [
                  {
                    "match_phrase": {
                      "japanese.text": {
                        "query": "\u80FD\u4EE3"
                      }
                    }
                  },
                  {
                    "match": {
                      "japanese.text": {
                        "minimum_should_match": "75%",
                        "query": "\u80FD\u4EE3"
                      }
                    }
                  },
                  {
                    "match": {
                      "japanese.text.conjugations": {
                        "minimum_should_match": "75%",
                        "query": "\u80FD\u4EE3"
                      }
                    }
                  }
                ]
              }
            }
            """);

        Add(because: "a query in English should search the English translation, even if it contains full-width spaces",
            query: "light　cruiser",
            json: /*lang=json,strict*/ """
            {
              "bool": {
                "minimum_should_match": 1,
                "should": [
                  {
                    "match_phrase": {
                      "english.text": {
                        "query": "light\u3000cruiser"
                      }
                    }
                  },
                  {
                    "match": {
                      "english.text": {
                        "minimum_should_match": "75%",
                        "query": "light\u3000cruiser"
                      }
                    }
                  },
                  {
                    "match": {
                      "english.text.conjugations": {
                        "minimum_should_match": "75%",
                        "query": "light\u3000cruiser"
                      }
                    }
                  }
                ]
              }
            }
            """);

        Add(because: """
            a query containing a single kanji should search the dedicated kanji subfield (since the regular field is
            restricted to bigrams)
            """,
            query: "𪚲", // This is also an example of a four-byte kanji (which, since .NET uses UTF-16, means the
                         // string has a length of 2)
            json: /*lang=json,strict*/ """
            {
              "match": {
                "japanese.text.kanji": {
                  "query": "\uD869\uDEB2"
                }
              }
            }
            """);

        Add(because: """
            @-mentions should be removed from the query and added as speaker name filters, with underscores replaced
            with spaces
            """,
            query: "rumors @Balgruuf_the_Greater",
            json: /*lang=json,strict*/ """
            {
              "bool": {
                "filter": {
                  "term": {
                    "english.speakerName.keyword": {
                      "value": "Balgruuf the Greater"
                    }
                  }
                },
                "minimum_should_match": 1,
                "should": [
                  {
                    "match_phrase": {
                      "english.text": {
                        "query": "rumors"
                      }
                    }
                  },
                  {
                    "match": {
                      "english.text": {
                        "minimum_should_match": "75%",
                        "query": "rumors"
                      }
                    }
                  },
                  {
                    "match": {
                      "english.text.conjugations": {
                        "minimum_should_match": "75%",
                        "query": "rumors"
                      }
                    }
                  }
                ]
              }
            }
            """);

        Add(because: """
            when an individual kanji is searched along with an @-mention, it should perform the kanji field search as
            usual but wrap it in a bool query to apply the speaker name filter (always using the English field)
            """,
            query: "炭 @Hachiroku",
            json: /*lang=json,strict*/ """
            {
              "bool": {
                "filter": {
                  "term": {
                    "english.speakerName.keyword": {
                      "value": "Hachiroku"
                    }
                  }
                },
                "must": {
                  "match": {
                    "japanese.text.kanji": {
                      "query": "\u70AD"
                    }
                  }
                }
              }
            }
            """);
    }

    private new void Add(string query, string json, string because) =>
        Add(new TheoryDataRow<string, string, string>(query, json, because)
        {
            TestDisplayName = because.ReplaceLineEndings(" ") + " "
        });
}
