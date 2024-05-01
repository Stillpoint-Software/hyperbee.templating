# Things TODO

It would be useful to support simple enumerations.

    {{each regex}}
        {{@value}} {{@index}}
    {{/each}}

    {{each x => IEnumeration<string>}}
        {{ x => x().value + x().index }}
    {{/each}}


