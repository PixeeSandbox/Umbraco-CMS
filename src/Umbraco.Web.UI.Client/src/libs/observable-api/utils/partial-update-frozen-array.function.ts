/**
 * @function partialUpdateFrozenArray
 * @param {Observable<T>} source - RxJS Subject to use for this Observable.
 * @param {T} data - Initial data for this Observable.
 * @param {Partial<T>} partialEntry - New data to be added in this Observable.
 * @param {(entry: T) => boolean} findMethod - Method to find the data to be updated.
 * @param {(mappable: T) => R} mappingFunction - Method to return the part for this Observable to return.
 * @param {(previousResult: R, currentResult: R) => boolean} [memoizationFunction] - Method to Compare if the data has changed. Should return true when data is different.
 * @returns {T[]} - New data set with the updated entry.
 * @description - Creates a RxJS Observable from RxJS Subject.
 * @example <caption>Example append new entry for a ArrayState or a part of UmbDeepState/UmbObjectState it which is an array. Where the key is unique and the item will be updated if matched with existing.</caption>
 * const partialEntry = {value: 'myValue'};
 * const newDataSet = partialUpdateFrozenArray(myState.getValue(), partialEntry, x => x.key === 'myKey');
 * myState.setValue(newDataSet);
 */
export function partialUpdateFrozenArray<T>(
	data: T[],
	partialEntry: Partial<T>,
	findMethod: (entry: T) => boolean,
): T[] {
	const unFrozenDataSet = [...data];
	const indexToReplace = unFrozenDataSet.findIndex((x) => findMethod(x));
	if (indexToReplace !== -1) {
		unFrozenDataSet[indexToReplace] = { ...unFrozenDataSet[indexToReplace], ...partialEntry };
	}
	return unFrozenDataSet;
}
