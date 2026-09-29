import type { Customer, Order, WireTypes, ReadRowsResult, Клиент, DoWorkParameters, DoWorkOutput, EchoParameters2, DefaultsParameters, ReadRowsParameters, ReadRowsOutput } from "../TypeScriptGenerated/Main/index";

type Equal<A, B> = (<T>() => T extends A ? 1 : 2) extends (<T>() => T extends B ? 1 : 2) ? true : false;
type Expect<T extends true> = T;
export type Checks = [
  Expect<Equal<WireTypes["id"], number>>,
  Expect<Equal<WireTypes["active"], boolean>>,
  Expect<Equal<WireTypes["optionalName"], string | null>>,
  Expect<Equal<WireTypes["createdAt"], string>>,
  Expect<Equal<WireTypes["duration"], string>>,
  Expect<Equal<WireTypes["blob"], string>>,
  Expect<Equal<WireTypes["largeId"], number>>,
  Expect<Equal<WireTypes["price"], number>>,
  Expect<Equal<WireTypes["variant"], unknown>>,
  Expect<Equal<WireTypes["constructor"], number>>,
  Expect<Equal<WireTypes["urlValue"], string>>,
  Expect<Equal<Order["customer"], Customer | null | undefined>>,
  Expect<Equal<Customer["ordersViaCustomer"], Order[] | null | undefined>>,
  Expect<Equal<Customer["manager"], Customer | null | undefined>>,
  Expect<Equal<ReadRowsResult["value"], number>>,
  Expect<Equal<Клиент["имя"], string>>,
  Expect<Equal<DoWorkParameters["class"], string | null | undefined>>,
  Expect<Equal<DoWorkParameters["cancellationToken"], number | null>>,
  Expect<Equal<DoWorkOutput["cancellationToken"], number | null>>,
  Expect<Equal<ReadRowsParameters["count"], number | null>>,
  Expect<Equal<ReadRowsOutput["count"], number | null>>,
  Expect<Equal<DefaultsParameters["last"], number | undefined>>,
  Expect<Equal<EchoParameters2["filter"], string | null | undefined>>
];

export function checkNullability(value: WireTypes): void {
  value.optionalName = null;
  // @ts-expect-error A nonnullable SQL int is not nullable in the generated contract.
  value.id = null;
  // @ts-expect-error JSON dates are strings; HttpClient does not convert them to Date instances.
  value.createdAt = new Date();
}

export const requestWithDefaults: DefaultsParameters = { first: null, last: 0 };
export const outputRequest: DoWorkParameters = { class: "text", cancellationToken: null, p0: null };
// @ts-expect-error An INPUT/OUTPUT argument must be explicitly assigned to capture its result.
export const missingOutput: DoWorkParameters = { class: "text" };
// @ts-expect-error Optional fields must be omitted, not explicitly set to undefined with exactOptionalPropertyTypes.
export const undefinedInput: EchoParameters2 = { filter: undefined };
