export default {
  witnes: {
    input: './openapi.json',
    output: {
      target: './src/generated/api.ts',
      client: 'axios',
      format: 'esm',
      override: {
        mutator: {
          path: './src/lib/axios-instance.ts',
          name: 'customInstance',
        },
      },
    },
    hooks: {
      afterAllFilesWrite: 'prettier --write',
    },
  },
};
