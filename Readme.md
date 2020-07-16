# Catutil

Tools for Catullus data drop pattern analysis and migration.

## Quick Start

1. have the XLS files with text and apparatus.
2. ensure your MySql server is available.
3. import the XLS files into MySql, e.g. (the first pass with `-d` is just a dry run for test):

```ps1
.\Catutil.exe import-text c:\users\dfusi\desktop\co\ *.xls catullus -d
.\Catutil.exe import-text c:\users\dfusi\desktop\co\ *.xls catullus
```

4. dump the imported database into Proteus entries:

```ps1
.\Catutil.exe parse-text catullus c:\users\dfusi\desktop\co\Dump.json
```

where the content of `Dump.json` is:

```json
{
  "EntryReader": {
    "Id": "entry-reader.co-sql"
  },
  "EntryFilters": [
    {
      "Id": "entry-filter.escape",
      "Options": {
        "EscapeDecoders": [
          {
            "Id": "escape-decoder.co-entry-id"
          },
          {
            "Id": "escape-decoder.co-italic"
          }
        ]
      }
    }
  ],
  "EntryRegionDetectors": [],
  "EntryRegionFilters": [
    {
      "Id": "region-filter.unmapped",
      "Options": {
        "UnmappedRegionTag": "x"
      }
    }
  ],
  "EntryRegionParsers": [
    {
      "Id": "entry-region-parser.excel-dump",
      "Options": {
        "MaxEntriesPerDumpFile": 10000,
        "OutputDirectory": "c:\\users\\dfusi\\desktop\\co\\dump\\"
      }
    }
  ]
}
```

## Excel Source Files Documentation

Text and apparatus are originated in a number of Excel files (XLS format).

Each of these files has a single sheet each, including 3 columns for each line's ID, text, and apparatus text. Usually each row corresponds to a single line; but sometimes a line row is followed by other rows lacking the line's ID and text, and representing the continuation of the apparatus text. 

As for the text patterns, these indications (slightly reworked for practical purposes) are from Daniel Kiss:

- conjectures and manuscript readings are in plain type, while manuscript sigla, bibliographical references and editorial explanations and comments are in italics. One should note the use of inverse italics (i.e. plain type) for the titles of works quoted within an editorial note. Thus (in these samples I represent italic between underscores) 1.1 `Cui _OGR_` (i.e. this is the reading of manuscripts OGR); 6.13 `nec tu _damn. A. Guarinus 1521 (quod fort. ipse coniecerat_)`, with editorial comment in cursive; also 1.4 `nostras _male Plinio_ (N.H. _praef. 1_) _attribuit Marcilius 1604 5 et Vossius 1684_`, with the title `N.H.` in inverse italics (i.e. plain type).
- different variant readings for the same passage are separated by ` : ` (see the 1.4 sample quoted above). Thus 1.4 `meas _OGR_ : nostras _male Plinio_ (N.H. _praef. 1_) _attribuit Marcilius 1604 5 et Vossius 1684_`.
- variant readings on different passages are separated by ` | ` (two spaces on each side of the pipe in the Excel tables, which have become a single space on either side in Catullus Online). Thus 1.3 `Corneli? tibi codd. plerique teste Ellis 1867 | uolebas Pleitner 1876 100`.
- variants that offer a minor modification to another variant are sometimes added within brackets. Thus 6.13 `non tam OGR (non? tam Spengel 1827 7)`.
- each conjecture or variant is added, inevitably, besides one verse of Catullus. Those conjectures or variants that affect several verses are added at the start of the entry on the first of those verses, the longest passage coming first.

This configures a hierarchy which in terms of the Cadmus MQDQ apparatus model can be represented as follows:

- variants on different passages (separated by pipes) correspond to fragments in the apparatus part.
- variants for the same passage (separated by colons) correspond to entries in a single fragment.

The `import-text` command can be used to create a MySql database filled with these entities.

## Command import-text

This command imports text and apparatus from the original Excel's (XLS) files into a MySql database, designed to just serve as a playground for pattern analysis targeted to automatic structuring in apparatus.

The source files have a single sheet each, including 3 columns for each line's ID, text, and apparatus text. Usually each row corresponds to a single line; but sometimes a line row is followed by other rows lacking the line's ID and text, and representing the continuation of the apparatus text. This is taken into account by the importer, which merges such rows into a single apparatus text.

The importer also behaves with a certain degree of tolerance with reference to occasional mistakes (e.g. comma instead of the usual dot in verse IDs) and Excel's editing artifacts (e.g. different cell types, either numeric or textual; or formatting artifacts like an italic word where also the preceding or following spaces are italicized).

Syntax:

```ps1
.\Catutil.exe import-text InputFilesDir InputFilesMask TargetDBName [-d]
```

where:

- `InputFilesDir`: the Excel XLS input files directory.
- `InputFilesMask`: the input files mask (e.g. `*.xls`).
- `TargetDBName`: the name of the target MySql database.
- the `-d` option (`--dry`) writes nothing to the database.

The target database has 3 tables:

- `line`: each row is a line of text, with these properties:
  - `id`: the line ID.
  - `poem`: the poem number (alphanumeric, but 0-padded to 3 digits in its initial part), derived from the id.
  - `ordinal`: the line ordinal number in the poem. This is derived from the order of the rows in the Excel files.
  - `value`: the value of the text, here corresponding to a line.

- `fragment`: each line is split into 1 or more fragments using the pipe separator. Each fragment has these properties:
  - `id`: an arbitrary numeric ID assigned by the database.
  - `lineId`: the ID of the line this fragment refers to.
  - `ordinal`: the ordinal number of the fragment in the line's apparatus.
  - `value`: the text value for the fragment.

- `entry`: each fragment is split into 1 or more entries using the colon separator. Each entry has these properties:
  - `id`: an arbitrary numeric ID assigned by the database.
  - `fragmentId`: the ID of the fragment this entry refers to.
  - `ordinal`: the ordinal number of the entry in the fragment.
  - `value`: the text value for the entry.

The apparatus entry is built as follows:

1. the text is read from the Excel's cell with its formatting as related to italic.
2. italic text is wrapped by `_`, as per Markdown convention, so that we can use a simple string to represent both the text and its formatting. Eventual `_` characters existing in the text are escaped into `\_`.
3. whitespaces in the text are normalized, i.e. any sequence of 1 or more whitespace characters (space, tab, etc.) becomes a single space, and whitespaces at the beginning or end of the text are removed.
4. italic markers (`_`) are adjusted to avoid having unnecessary italicized spaces, i.e. in `Cui_ OGR_...` where the italic is extended in Excel also to the space preceding `OGR` becomes `Cui _OGR_...`. This removes the most evident editing artifacts, providing a more polished text to analysis. This anyway does not happen when a pipe is involved, as it's a separator and cannot be modified.
5. the text is split at each sequence of space + pipe (`|`) + space, thus providing 1 or more apparatus fragments.
6. in turn, each apparatus fragment is split at each sequence of space + colon + space, thus providing 1 or more fragment's entries.

For instance, here is the first line of poem 1:

- `id`: `1.1`
- `poem`: `001`
- `ordinal`: `1`
- `value` = `Cui dono lepidum nouom libellum`

Its corresponding apparatus fragments are 3 (I omit the `id` value as its meaningful only inside the target database), each having its entries:

(1) fragment 1

- `lineId`: `1.1`
- `ordinal`: `1`
- `value`: `Cui _OGR_, _Scholia Veronensia in Verg._ Ecl. _6.1_, _Caesius Bassus_ GL _6.261.21_, _Aphthonius_ GL _6.148.22, Terentianus Maurus_ De Metris _2562, Isid._ Orig. _6.12.3, Auson._ Ecl. _1.1_ : Qui _Pastrengicus_ De Originibus Rerum _88v ed. Veneta_, _MS. 12 a. 1445 ca., MS. 1 a. 1451, prob. Munro 1872a, Ellis 1878_ : Quoi _MS. 98 a. 1450 ca., MS. 122 a. 1460_ : quui _MS. 9 a. 1465 ca._ : cuoi _MS. 13 a. 1474_ : quin _MS. 'Patavinus alter' teste Statio 1566, at ego D.K. hunc codicem reperire nequiui`

(1.1) entry 1

- `ordinal`: `1`
- `value`: `Cui _OGR_, _Scholia Veronensia in Verg._ Ecl. _6.1_, _Caesius Bassus_ GL _6.261.21_, _Aphthonius_ GL _6.148.22, Terentianus Maurus_ De Metris _2562, Isid._ Orig. _6.12.3, Auson._ Ecl. _1.1_`

(1.2) entry 2

- `ordinal`: `2`
- `value`: `Qui _Pastrengicus_ De Originibus Rerum _88v ed. Veneta_, _MS. 12 a. 1445 ca., MS. 1 a. 1451, prob. Munro 1872a, Ellis 1878_`

(1.3) entry 3

- `ordinal`: `3`
- `value`: `Quoi _MS. 98 a. 1450 ca., MS. 122 a. 1460_`

(1.4) entry 4

- `ordinal`: `4`
- `value`: `quui _MS. 9 a. 1465 ca._`

(1.5) entry 5

- `ordinal`: `5`
- `value`: `cuoi _MS. 13 a. 1474_`

(1.6) entry 6

- `ordinal`: `6`
- `value`: `quin _MS. 'Patavinus alter' teste Statio 1566, at ego D.K. hunc codicem reperire nequiui`

(2) fragment 2

- `lineId`: `1.1`
- `ordinal`: `2`
- `value`: `_lepidum <et> '_poterat ... scripsisse, si nostro more ineptire uoluisset' ita Fruterius_ (†_1566_) _1605 341_`

(2.1) entry 1

- `ordinal`: `1`
- `value`: `_lepidum <et> '_poterat ... scripsisse, si nostro more ineptire uoluisset' ita Fruterius_ (†_1566_) _1605 341_`

(3) fragment 3

- `lineId`: `1.1`
- `ordinal`: `3`
- `value`: `nouom _Postgate 1893a in contextu_, _Friedrich 1908_ : nouum _OGR_ : meum _MS. 19 a. 1450 ca., MS 45 a. 1465 ca._`

(3.1) entry 1

- `ordinal`: `1`
- `value`: `nouom _Postgate 1893a in contextu_, _Friedrich 1908_`

(3.2) entry 2

- `ordinal`: `2`
- `value`: `nouum _OGR_`

(3.3) entry 3

- `ordinal`: `3`
- `value`: `meum _MS. 19 a. 1450 ca., MS 45 a. 1465 ca._`

## Command parse-text

This command parses the text from each fragment entry in the Catullus database dump.

Syntax:

```ps1
.\Catutil.exe parse-text SourceDBName PipelineConfigPath OutputDir
```

where:

- `SourceDBName`: source MySql database name. This database is the one generated by importing XLS files using the `import-text` command.
- `PipelineConfigPath`: the path to the parser pipeline configuration JSON file.
- `OutputDir`: the output directory.
