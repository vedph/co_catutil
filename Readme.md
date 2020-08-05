# Catutil

Tools for Catullus data drop pattern analysis and migration.

## Concept

The legacy CO (Catullus Online) edition by Daniel Kiss is based on a RDBMS database which in turn gets populated from a set of flat Excel files (legacy XLS format).

These files essentially have a very simple structure, where the atomic unit is the single line. Each line usually corresponds to a row in the spreadsheet document; this row has just 3 columns, including:

- poem and line number;
- line's text;
- apparatus' text. This is the full text for all the apparatus entries referred to that line. This apparatus also includes italic and non italic text regions.

Starting from this core resource (and other easy-to-port data like bibliography), we want to:

- **transform** the apparatus text into structured data, using a more abstract and granular model.
- **edit** in a web-based, multiple-user context the resulting database, by adding new data and new data types, and fixing issues.
- **generate** at least these outputs:
  - a TEI-based, MQDQ-compliant output to insert the new edition of Catullus into MQDQ.
  - legacy XLS files to feed the legacy CO software.

To this end, the general plan is:

- **refactor** the text from XLS files into a full-fledged, highly structured Cadmus database, ready for editing.
- **edit** the database in Cadmus.
- **export** data from the database into MQDQ TEI files (text and standoff apparatus).
- **export** data from the database into CO XLS files, to feed the legacy software with up-to-date data.

MQDQ is already following a similar flow: it imports TEI documents (text and standoff apparatus), and remodels data into a higher abstraction level structure; data is edited in Cadmus; finally, data is exported back into TEI. So, we are essentially going to use the same modeling for both MQDQ and CO.

The main issue here is posed by the low structuring level of the original (XLS-based) CO apparatus, which is just a typographically formatted text, and should rather be semantically remodeled, with higher granularity. We thus have to focus on importing CO data into a Cadmus-based database.

### Importing CO Data

As for text and apparatus, importing CO data happens in two main steps:

1. read the original XLS files and remodel them into a simple RDBMS database (here **MySql**). This allows analyzing and searching data with ease, thus providing a tool for detecting patterns. This first step splits the apparatus into groups of entries (corresponding to Cadmus layer part fragments) and single entries (each corresponding to a fragment's entry). This can be done easily, as the apparatus follows some conventions (pipes and colons) for signaling text divisions. Also, the typographic formatting (italic vs non-italic) is read and rewritten into Markdown conventions (underscores wrapping italic text).

2. read each apparatus entry, as split and rewritten by the previous step, and incrementally apply **parsing**, so that semantic roles can be inferred from a combination of typographic formatting, text content, and context.

The parsing process is based on a bigger infrastructure (codenamed _Proteus_) I created for other projects, requiring to remodel heavily typographically marked texts into semantic structures. Proteus has been applied to real-world projects related to big bilingual or monolingual dictionaries, documental archives, and paper-based critical editions (see D. Fusi, *Recovering Legacy in the Digital World: Tales and Tools*, «Rationes Rerum» 12 (2018) 203-262). Its main purpose is providing a framework to compose an incrementally built parsing pipeline, especially fit to complex texts where scarce or no documentation is available. In these cases, one usually starts with a few hypotheses about the most evident semantic roles inside the original text; then, he goes on by progressively refining and adding new detection rules, using a number of prebuilt or custom built modules which can chained at will. This allows to heuristically define a full parsing process, when you can examine the results at each single repetition pass, from the very beginning up to the end.

The Proteus-based parsing pipeline includes any number of different types of modular software components; it is fully defined in a JSON file, where types, order and parameters of each component are specified.

## XLS Source Files

To start with, text and apparatus are originated in a number of Excel files (XLS format).

Each of these files has a single sheet each, including 3 columns for each line's ID, text, and apparatus text. Usually each row corresponds to a single line; but sometimes a line row is followed by other rows lacking the line's ID and text, and representing the continuation of the apparatus text.

As for the text patterns, these indications (slightly reworked for practical purposes) are from Daniel Kiss:

- conjectures and manuscript readings are in plain type, while manuscript sigla, bibliographical references and editorial explanations and comments are in italics. One should note the use of inverse italics (i.e. plain type) for the titles of works quoted within an editorial note. Thus (in these samples I represent italic between underscores) 1.1 `Cui _OGR_` (i.e. this is the reading of manuscripts OGR); 6.13 `nec tu _damn. A. Guarinus 1521 (quod fort. ipse coniecerat_)`, with editorial comment in cursive; also 1.4 `nostras _male Plinio_ (N.H. _praef. 1_) _attribuit Marcilius 1604 5 et Vossius 1684_`, with the title `N.H.` in inverse italics (i.e. plain type).
- different variant readings for the same passage are separated by `:` (see the 1.4 sample quoted above). Thus 1.4 `meas _OGR_ : nostras _male Plinio_ (N.H. _praef. 1_) _attribuit Marcilius 1604 5 et Vossius 1684_`.
- variant readings on different passages are separated by `|` (two spaces on each side of the pipe in the Excel tables, which have become a single space on either side in Catullus Online). Thus 1.3 `Corneli? tibi codd. plerique teste Ellis 1867 | uolebas Pleitner 1876 100`.
- variants that offer a minor modification to another variant are sometimes added within brackets. Thus 6.13 `non tam OGR (non? tam Spengel 1827 7)`.
- each conjecture or variant is added, inevitably, besides one verse of Catullus. Those conjectures or variants that affect several verses are added at the start of the entry on the first of those verses, the longest passage coming first.

This configures a hierarchy which in terms of the Cadmus MQDQ apparatus model can be represented as follows:

- variants on different passages (separated by pipes) correspond to fragments in the apparatus part.
- variants for the same passage (separated by colons) correspond to entries in a single fragment.

The `import-text` command can be used to create a MySql database filled with these entities.

## Parsing Apparatus

### Introducing Proteus

As for parsing, I tried to adopt a Proteus-based strategy, which has the advantage of leveraging a proven framework, built for scenarios like this.

Essentially, Proteus reduces any source into a common model, represented by a flat list of "entries". A Proteus entry is either a piece of plain text, or a formatting property (e.g. italic), or more complex metadata conveyed by what I call "commands" with their arguments. You can imagine all this as the set of instructions to some rendering device, where you tell it to print some text (text), switch to italic (property), move to the next sheet (command), etc. So, there are just 3 types of entries: text, property, and command. Source data, whatever its format (word processor, spreadsheet, database, etc.) is reduced to a list of such entries.

  Please notice that the term _entry_ here is used in two very different contexts: in the context of Cadmus layer models, an entry is the item of an array of models in a layer part's fragment; in turn, a fragment is a set of models referred to the same portion of the base text. In the context of Proteus transformations, an _entry_ is an atomic piece of data from a text, whether it's just a text, a formatting property, or a more complex command.

The typical strategy is looking at the list and defining *regions* inside it, i.e. sequences of entries belonging to the same semantic unit. For instance, a region might be the list of witnesses (manuscripts) like the italic "OGR"; another the lemma in a variant; another a comment; etc. We want to define all the regions required to build the target Cadmus models for the apparatus.

Once we have these regions, we can take a specialized action for each of them, which allows the software to extract the data, remodel them, and store the result in the target database. That's the task of components named region *parsers*. Each region can be handled by a specific parser.

We thus have a pipeline where several different components take their place: we start from reading a source entry by entry; we can then eventually filter these entries; then we detect regions; we can then filter these regions; and finally we parse the result.

The concept is building this pipeline since the very beginning of our analysis, and then progressively enrich it by adding new components and refining their configuration. We can thus explore the data at the same time we are transforming it. To this end, the last component in the current pipeline is a dump parser, i.e. something which does not effectively remodel and store data, but just dumps the list of entries got from executing the pipeline up to that point. These dumps are in form of a set of Excel files.

### The Proteus CO Pipeline

Parsing apparatus starts at the most atomic level we can attain from the input text as imported in the MySql database, i.e. the single entry (in the meaning defined above) in an apparatus. Thus, the input for the pipeline is just the MySql database got from the previous step in our flow.

A first pipeline is built to provide as its output a detailed dump of the text being analyzed, in the form of a number of Excel files (XLSX). The dump is split into several Excel files only to avoid making them too big; at about 10,000 lines a new file is created, yet taking care not to split the dump of a single entry.

If you open an Excel dump of these, you can see that each source text starts with a row with its ordinal number (#1, #2, etc.), in a yellow background.

Inside each of these sets (each corresponding to a Cadmus entry), each Proteus entry is found in a row:

- the `type` column tells you the entry type (text, property, command); if it's a text, the `txt` column contains it. If it's a property, the `pn`/`pv` columns tell you the property name and value (e.g. `italic=1`). If it's a command, the `cmd` column contains it with its arguments
- the `rgn` column tells the name(s) of all the regions including that entry.

For instance, consider this apparatus' entry:

```txt
Cui _OGR_, _Scholia Veronensia in Verg._ Ecl. _6.1_, _Caesius Bassus_ GL _6.261.21_, _Aphthonius_ GL _6.148.22, Terentianus Maurus_ De Metris _2562, Isid._ Orig. _6.12.3, Auson._ Ecl. _1.1_
```

This is the first entry of the group of entries in the apparatus text related to Catullus poem 1 line 1. Underscores here toggle italic, as per Markdown. This entry is the result of importing the XLS files into a RDBMS.

The first Proteus entries got from the pipeline for the above sample text are like those:

```txt
cmd set-ids(f=1, e=1)
txt Cui
prp italic=1
txt OGR
prp italic=0
txt ,
prp italic=1
txt Scholia Veronensia in Verg.
prp italic=0
...
```

Here we start with a command (`cmd`) which sets the IDs of this set (f=fragment ID, e=entry ID, both in the RDBMS).

We then have an initial text (`Cui`) followed by the italic text `OGR` (wrapped in two properties -`prp`- entries, which set the italic property on and off); then a comma follows, and then another italic piece of text.

As you can see, the input text is now represented by a list of Proteus entries (text, properties, and commands).

In our workflow, a first, almost empty Proteus pipeline was set up to just spit out some Excel dumps, which list such entries. This pipeline contains these components (applied in this order):

1. **entry reader**: a component which reads the imported RDBMS database entry by entry, and outputs a Proteus entry at a time. This is at the start of the pipeline, as each entry being output feeds the next modules in it.

2. **entry filters**: a number of components used to filter the list of entries got from the entry reader (one at a time); in this case, we use a couple of escape filters to extract the database IDs (required to later provide the basis for a real data import) and convert Markdown underscores into property entries which toggle italic.

3. **region detectors**: a number of components used to detect semantically marked regions inside the list of Proteus entries. For instance, to start with the easiest region, consider the text representing manuscript witnesses: it is italic, and it should contain only the letters of 1 or more manuscripts, which in our case are just `O`, `G`, and `R`. Thus, we can define this pattern for detecting it:

- italic on;
- text including only any letter from `OGR`;
- italic off.

This is right what is done by this fragment in the Proteus pipeline definition:

```json
  "EntryRegionDetectors": [
    {
       "Id": "region-detector.pattern",
       "Options": {
          "Tag": "wit",
          "IsWholeRegionInA": true,
          "PatternEntriesA": [
            "prp italic=1",
            "txt$^[OGR]+$",
            "prp italic=0"
          ]
        }
    }
  ],
```

Here you can see that we are using a pattern-based region detector, which detects a region called "wit" (=witnesses) when the above pattern gets matched. The result is that any occurrence of these 3 Proteus entries get wrapped inside a `wit` region.

The concept here is adding as many region detectors required to split the list of Proteus entries into semantically distinct subsets; later, a set of parser components will be able to parse each of these regions according to their nature and content, producing the data to be imported in Cadmus. This could be thought similar to text markup, but here we are not changing the text in any way, and our regions can freely nest and even overlap. This is only a way of segmenting text to later apply context-specific parsing to each detected region.

4. **region filters**: a number of components used to refine the detected regions by filtering them. For instance, here we are filtering any subset of entries not wrapped into a region (in our sample, every entry except those under `wit`, which is the only region being detected) to wrap it into an "unknown" region named `x`. Here is the corresponding pipeline definition:

```json
  "EntryRegionFilters": [
    {
      "Id": "region-filter.unmapped",
      "Options": {
        "UnmappedRegionTag": "x"
      }
    }
  ],
```

Thus, after this stage we expect that every Proteus entry either belongs to a `wit` or an `x` region. We do this because the region-based pipeline ends with a set of parsers targeting regions, so that any portion of text we want to take into account should be wrapped inside some region.

5. **region parsers**: a number of components used to parse each detected region, providing the adequate actions for extracting and remodeling data from it as required by the target format. In the sample pipeline, we are using a parser component which does not really do any parsing, and gets applied to any region. Its only purpose is dumping the entries into Excel files, so that users can look at them to empirically define patterns and check the outcome of the pipeline:

```json
  "EntryRegionParsers": [
    {
      "Id": "entry-region-parser.excel-dump",
      "Options": {
        "MaxEntriesPerDumpFile": 10000,
        "OutputDirectory": "c:\\users\\dfusi\\desktop\\co\\dump\\"
      }
    }
  ]
```

Thus, executing this Proteus pipeline produces a set of Excel dumps from a MySql database. These dumps are an extremely useful tool for defining patterns, and thus building a parser capable of deducing the semantic role of each portion of text, whatever its original form and content.

Once the pipeline has been completed and tested, we can just replace the dump parser with the parsers required for our regions to effectively import structured apparatus data into a Cadmus database.

This defines a way of transforming data which can easily be approached by non specialists, in a heuristic, progressively refined processing flow. Proteus provides a lot of prebuilt modules for the pipeline, but being a modular framework it allows you to add your own modules wherever required, thus covering any complex input text. In most cases anyway you just end up defining the pipeline in a JSON file, and configuring its modules as needed.

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

### Command parse-app

This command parses the apparatus text from each fragment entry in the Catullus database, using a specified Proteus pipeline.

Syntax:

```ps1
.\Catutil.exe parse-app SourceDBName PipelineConfigPath OutputDir
```

where:

- `SourceDBName`: source MySql database name. This database is the one generated by importing XLS files using the `import-text` command.
- `PipelineConfigPath`: the path to the parser pipeline configuration JSON file.
- `OutputDir`: the output directory.

Example:

```ps1
.\Catutil.exe parse-app catullus c:\users\dfusi\desktop\co\ProteusDump.json c:\users\dfusi\desktop\co\dump\
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
