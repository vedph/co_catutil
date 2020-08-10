# Catutil

Tools for Catullus data drop pattern analysis and migration. Please refer to [this project's Wiki](https://github.com/vedph/co_catutil/wiki) for the documentation.

## Tool Commands

### Command import-text

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

Example:

```ps1
parse-xls-text catullus c:\users\dfusi\desktop\co\Dump.json
```

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

### Command dump-text

This command dumps Excel texts into the console. It is a diagnostic command to quickly inspect the Excel reading capabilities.

Syntax:

```ps1
.\Catutil.exe dump-text InputDirectory ExcelFilesMask
```

Example:

```ps1
.\Catutil.exe dump-text c:\users\dfusi\desktop\co\ *Repertory*.xls
```

### Command parse-txt

This command parses poems text lines from the MySql database into a set of dump JSON files, including an array of Cadmus items, each with its tiled text part. Also, the `itemId` column in the `line` table of the source database is updated so that it reflects the mapping between lines and their Cadmus items.

Syntax:

```ps1
.\Catutil.exe parse-txt SourceDatabaseName OutputDirectory [-m MaxItemPerFile] [-n]
```

where:

- `SourceDatabaseName`: the source database name.
- `OutputDirectory: the output directory.
- `MaxItemPerFile`: maximum count of items per output file. Default is 100.
- `-n`: add this option to avoid updating the `itemId` column.

Example:

```ps1
.\Catutil.exe parse-txt catullus c:\users\dfusi\desktop\co\items\
```

### Command parse-app

This command parses the apparatus text from each fragment entry in the Catullus database, using a specified Proteus pipeline.

Syntax:

```ps1
.\Catutil.exe parse-app SourceDBName PipelineConfigPath
```

where:

- `SourceDBName`: source MySql database name. This database is the one generated by importing XLS files using the `import-text` command.
- `PipelineConfigPath`: the path to the parser pipeline configuration JSON file.

Example:

```ps1
.\Catutil.exe parse-app catullus c:\users\dfusi\desktop\co\ProteusDump.json
```

### Command build-biblio

This command builds a bibliography JSON lookup data file from the source XLS bibliography file, and also dumps all the bibliographic references which can be built from that data. The JSON data file will be named `biblio-lookup.json`; the dump file will be named `biblio-lookup-dump.txt`.

Syntax:

```ps1
.\Catutil.exe build-biblio InputXlsFilePath OutputDirectory
```

Example:

```ps1
.\Catutil.exe build-biblio "c:\users\dfusi\desktop\co\4_1 Bibliography.xls" c:\users\dfusi\desktop\co\
```
