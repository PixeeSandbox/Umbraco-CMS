import { UmbDataSourceDataMapper, type UmbDataSourceDataMapperMapArgs } from '../data-mapper.js';
import { UMB_MANAGEMENT_API_DATA_SOURCE_ALIAS } from './constants.js';
import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';

export class UmbManagementApiDataMapper extends UmbControllerBase {
	#dataMapper = new UmbDataSourceDataMapper(this);

	constructor(host: UmbControllerHost) {
		super(host);
	}

	map(args: Omit<UmbDataSourceDataMapperMapArgs, 'forDataSource'>) {
		return this.#dataMapper.map({
			...args,
			forDataSource: UMB_MANAGEMENT_API_DATA_SOURCE_ALIAS,
		});
	}
}
