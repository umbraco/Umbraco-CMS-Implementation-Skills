# MCP read results: fresh install

Recorded from `@umbraco-cms/mcp-dev` 17.6.8 connected to a fresh Umbraco 17.5.3 install with no
starter kit, with the `document-type`, `data-type`, `template` and `language` collections enabled.
Treat it as what the connected server reports. Nothing has been written to the site.

## Connected tools

The same document-type, data-type, template and language tools as any 17.6.8 server, including
`get-all-document-types`, `get-document-type-by-id`, `get-document-types-by-id-array`,
`create-document-type`, `update-document-type`, `create-document-type-folder`, `get-icons`,
`find-data-type`, `get-data-type`, `get-template-root`, `create-template`, `get-language`.

## get-all-document-types

```json
[]
```

## get-template-root

```json
{ "items": [], "total": 0 }
```

## find-data-type (built-in data types only; no custom data types or folders)

| Name | Editor | Id |
|---|---|---|
| Textstring | `Umbraco.TextBox` | `0cc0eba1-9960-42c9-bf9b-60e150b429ae` |
| Textarea | `Umbraco.TextArea` | `c6bac0dd-4ab9-45b1-8e30-e4b619ee5da3` |
| Richtext editor | `Umbraco.RichText` | `ca90c950-0aff-4e72-b976-a30b1ac57dad` |
| True/false | `Umbraco.TrueFalse` | `92897bc6-a5f3-4ffe-ae27-f2e7e33dda49` |
| Image Media Picker | `Umbraco.MediaPicker3` | `ad9f0cf2-bda2-45d5-9ea1-a63cfc873fd3` |

## get-language

```json
{ "items": [ { "isoCode": "en-GB", "name": "English (United Kingdom)", "isDefault": true } ], "total": 1 }
```
