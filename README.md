Primitive console app to translate one ResX file to another via ChatGPT.

Files must be already created manually!
Default locale is English!

## Usage examples:

Translate from ru to es.
```
ResxTranslator.exe --dir "C:\src\MyApp\Resources" --from "ru" --to "es"
```

Translate from Default to es.
```
ResxTranslator.exe --dir "C:\src\MyApp\Resources" --to "es"


Translate from Default to all languages
```
ResxTranslator.exe --dir "C:\src\MyApp\Resources"
```

Translate from ru to all languages
```
ResxTranslator.exe --dir "C:\src\MyApp\Resources" --from "ru"
```

you can pass --api-key as argument or set API_KEY enviroment variable for ChatGPT.


